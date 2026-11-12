import assert from 'node:assert/strict';
import { readFile, writeFile } from 'node:fs/promises';
import { chromium, expect } from '@playwright/test';

const site = 'https://staging.mzcampuspulse.com';
const evidence = process.env.MCP_EVIDENCE_DIR || 'docs/evidence/staging-mcp';
const browser = await chromium.launch();
const readScopes = ['mcp.search_trips', 'mcp.get_trip', 'mcp.get_stop_arrivals'];
const writeScopes = ['trips.write', 'mcp.preview_trip_update', 'mcp.record_trip_update'];

async function login(role, scopes) {
  const identity = JSON.parse(await readFile('.local-secrets/oidc-smoke/' + role + '.json', 'utf8'));
  const page = await browser.newPage();
  await page.route('https://dev-okl8jqszwoasg244.us.auth0.com/authorize?**', route => {
    const url = new URL(route.request().url());
    url.searchParams.set('scope', ['openid', ...scopes].join(' '));
    return route.continue({url: url.href});
  });
  await page.goto(site);
  await page.getByRole('button', {name: 'Sign in', exact: true}).click();
  await page.locator('input[name=username]:visible, input[type=email]:visible').first().fill(identity.email);
  const password = page.locator('input[type=password]:visible');
  if (!await password.count()) await page.getByRole('button', {name: 'Continue', exact: true}).click();
  await password.fill(identity.password);
  await page.getByRole('button', {name: 'Continue', exact: true}).click();
  const consent = page.getByRole('button', {name: /^(Accept|Authorize|Allow)$/i}).first();
  await Promise.race([page.waitForURL(site + '/**'), consent.waitFor({state: 'visible'})]);
  if (!page.url().startsWith(site)) await consent.click();
  await page.waitForURL(site + '/**');
  await page.waitForFunction(() => Object.keys(sessionStorage).some(key => key.startsWith('oidc.user:')));
  const token = await page.evaluate(() => {
    const key = Object.keys(sessionStorage).find(key => key.startsWith('oidc.user:'));
    return JSON.parse(sessionStorage.getItem(key)).access_token;
  });
  await page.close();
  return token;
}

let sequence = 0;
async function rpc(token, method, params) {
  const headers = {'Content-Type': 'application/json', Accept: 'application/json, text/event-stream'};
  if (token) headers.Authorization = 'Bearer ' + token;
  const response = await fetch(site + '/mcp', {method: 'POST', headers,
    body: JSON.stringify({jsonrpc: '2.0', id: ++sequence, method, params})});
  if (!token) return response.status;
  assert.equal(response.status, 200);
  const body = await response.text();
  const data = body.startsWith('{') ? body : body.split('\n').find(line => line.startsWith('data:')).slice(5);
  const message = JSON.parse(data);
  if (message.error) throw Error('MCP protocol error ' + message.error.code);
  return message.result;
}
const call = (token, name, args) => rpc(token, 'tools/call', {name, arguments: args});
async function check(token, name, args, shouldFail = false) {
  const result = await call(token, name, args);
  assert.equal(Boolean(result.isError), shouldFail, name);
  console.log(name, shouldFail ? 'denied' : 'succeeded');
  return result;
}

try {
  assert.equal(await rpc(null, 'tools/call', {name: 'search_trips', arguments: {}}), 401);
  console.log('Anonymous read HTTP 401');
  const viewer = await login('viewer', readScopes);
  const dispatcher = await login('dispatcher', [...readScopes, ...writeScopes]);
  const catalog = await (await fetch(site + '/api/catalog')).json();
  const created = await fetch(site + '/api/trips', {method: 'POST',
    headers: {Authorization: 'Bearer ' + dispatcher, 'Content-Type': 'application/json'},
    body: JSON.stringify({routeId: catalog.routes[0].id, vehicleId: catalog.vehicles[0].id, scheduledDeparture: new Date().toISOString()})});
  assert.equal(created.status, 201);
  const trip = await created.json();
  const proposal = {tripId: trip.id, version: trip.version, kind: 'delay', delayMinutes: 4};
  console.log('Synthetic trip', trip.id, 'initial version', trip.version);
  await check(viewer, 'search_trips', {search: trip.routeName});
  await check(viewer, 'get_trip', {tripId: trip.id});
  await check(viewer, 'get_stop_arrivals', {stopId: trip.stops[0].stopId});
  await check(viewer, 'preview_trip_update', proposal, true);
  const preview = await check(dispatcher, 'preview_trip_update', proposal);
  const data = preview.structuredContent ?? JSON.parse(preview.content[0].text);
  const confirmation = {confirmed: true, confirmationToken: data.confirmationToken};
  assert.equal(typeof confirmation.confirmationToken, 'string');
  await check(viewer, 'record_trip_update', confirmation, true);
  await check(dispatcher, 'record_trip_update', {...confirmation, delayMinutes: 10}, true);
  async function verify(version, delay) {
    const saved = await (await fetch(site + '/api/trips/' + trip.id)).json();
    assert.equal(saved.version, version);
    assert.equal(saved.delayMinutes, delay);
    console.log('Persisted trip', trip.id, 'version', version, 'total delay', delay);
  }
  await verify(trip.version, 0);
  const resource = await rpc(dispatcher, 'resources/read', {uri: 'ui://routeboard/mcp-app.html'});
  const host = await browser.newPage({recordVideo: {dir: evidence}, viewport: {width: 940, height: 680}});
  const video = host.video();
  // The host owns the bearer token. The sandboxed iframe receives only the tool result.
  await host.exposeFunction('confirmTrip', async args => {
    assert.deepEqual(args, confirmation);
    return check(dispatcher, 'record_trip_update', args);
  });
  await host.setContent('<iframe title="RouteBoard MCP App" sandbox="allow-scripts" style="width:100%;height:640px;border:0"></iframe>');
  await host.evaluate(({html, preview}) => {
    const frame = document.querySelector('iframe');
    const send = message => frame.contentWindow.postMessage({jsonrpc:'2.0', ...message}, '*');
    window.addEventListener('message', async event => {
      if (event.source !== frame.contentWindow || event.origin !== 'null') return;
      const {id, method, params} = event.data;
      if (method === 'ui/initialize') send({id, result: {protocolVersion: params.protocolVersion,
        hostInfo: {name:'RouteBoard staging test host', version:'1'}, hostCapabilities:{serverTools:{}}, hostContext:{}}});
      if (method === 'ui/notifications/initialized') send({method:'ui/notifications/tool-result', params: preview});
      if (method === 'tools/call' && params.name === 'record_trip_update')
        send({id, result: await window.confirmTrip(params.arguments)});
    });
    frame.srcdoc = html;
  }, {html: resource.contents[0].text, preview});
  const app = host.frameLocator('iframe');
  await expect(app.getByText(/Total delay: 4 minutes/)).toBeVisible();
  const frame = host.frames().find(child => child.parentFrame() === host.mainFrame());
  const axe = await readFile(new URL('../node_modules/axe-core/axe.min.js', import.meta.url), 'utf8');
  await frame.evaluate(axe);
  const audit = await frame.evaluate(async () => {
    const result = await window.axe.run(document);
    return {engine:result.testEngine, timestamp:result.timestamp, violations:result.violations,
      incompleteRules:result.incomplete.map(rule => rule.id), passedRules:result.passes.map(rule => rule.id)};
  });
  await writeFile(evidence+'/app-axe.json', JSON.stringify(audit,null,2)+'\n');
  assert.ok(!audit.violations.some(rule => ['serious','critical'].includes(rule.impact)));
  const confirm = app.getByRole('button', {name:'Confirm this exact update'});
  await host.keyboard.press('Tab');
  await expect(confirm).toBeFocused();
  await host.keyboard.press('Tab');
  await expect(app.getByRole('button',{name:'Cancel'})).toBeFocused();
  await host.keyboard.press('Shift+Tab');
  await expect(confirm).toBeFocused();
  await host.keyboard.press('Enter');
  await expect(app.getByText('The confirmed update was saved.')).toBeVisible();
  await host.close();
  await video.saveAs(evidence + '/host-confirmation.webm');
  await video.delete();
  await check(dispatcher, 'record_trip_update', confirmation, true);
  await verify(trip.version + 1, 4);
} finally {
  await browser.close();
}
