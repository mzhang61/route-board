import { chromium, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { readFile, writeFile } from 'node:fs/promises';

const site = 'https://staging.mzcampuspulse.com';
const output = 'docs/evidence/accessibility';
const browser = await chromium.launch();
const context = await browser.newContext();
const page = await context.newPage();
const scans = [];
const steps = [];

async function tabTo(locator) {
  await locator.waitFor({state: 'visible'});
  for (let count = 0; count < 100; count++) {
    if (await locator.evaluate(element => element === document.activeElement)) return;
    await page.keyboard.press('Tab');
  }
  throw Error('Keyboard could not reach ' + locator);
}

async function activate(locator) {
  await tabTo(locator);
  await page.keyboard.press('Enter');
}

async function scan(state) {
  const result = await new AxeBuilder({page}).analyze();
  scans.push({state, timestamp: result.timestamp, engine: result.testEngine, violations: result.violations,
    incompleteRules: result.incomplete.map(rule => rule.id), passedRules: result.passes.map(rule => rule.id)});
  await writeFile(output + '/axe.json', JSON.stringify(scans, null, 2) + '\n');
  console.log(state, 'violations:', result.violations.map(rule => rule.id + '/' + rule.impact));
  expect(result.violations.filter(rule => ['critical','serious'].includes(rule.impact))).toEqual([]);
}

try {
  await page.goto(site, {waitUntil: 'domcontentloaded'});
  const release = (await (await page.request.get(site + '/release.txt')).text()).trim();
  const search = page.getByLabel('Search route or vehicle');
  const tripButton = page.getByRole('button', {name: /^View trip /}).first();
  await tripButton.waitFor();
  await scan('public-board');
  await tabTo(search);
  await page.keyboard.type('no-such-synthetic-route');
  await page.keyboard.press('Enter');
  await expect(page.getByText('No trips match these filters.')).toBeVisible();
  steps.push('Tab to search, type an unmatched route, press Enter: empty state appears.');
  await scan('empty-search');
  await tabTo(search);
  await page.keyboard.press('ControlOrMeta+a');
  await page.keyboard.type('Synthetic');
  await tabTo(page.locator('select[name=routeId]'));
  await page.keyboard.press('ArrowDown');
  await activate(page.getByRole('button', {name:'Search trips'}));
  await activate(tripButton);
  await expect(page.getByRole('heading', {name:/^Trip [0-9]+:/})).toBeVisible();
  steps.push('Replace search, select a route with the arrow key, submit, open a trip with Enter.');
  await scan('trip-detail');
  await activate(page.getByRole('button', {name:'Close trip'}));
  await expect(page.getByRole('button', {name:'Close trip'})).toHaveCount(0);
  steps.push('Tab to Close trip and press Enter: detail closes.');
  await page.route('**/api/trips?**', route => route.fulfill({status:503, json:{status:503}}), {times:1});
  await tabTo(search);
  await page.keyboard.press('ControlOrMeta+a');
  await page.keyboard.press('Backspace');
  await page.keyboard.press('Enter');
  await expect(page.getByRole('alert')).toContainText('could not be loaded');
  await scan('recoverable-error');
  await activate(page.getByRole('button', {name:'Retry'}));
  await tripButton.waitFor();
  steps.push('Force one trip-list 503 in the browser. Tab to Retry, press Enter: trips return.');

  const user = JSON.parse(await readFile('.local-secrets/oidc-smoke/dispatcher.json','utf8'));
  await activate(page.getByRole('button', {name:'Sign in', exact:true}));
  const email = page.locator('input[name=username]:visible, input[type=email]:visible').first();
  await tabTo(email);
  await page.keyboard.type(user.email);
  const password = page.locator('input[type=password]:visible');
  if (!await password.count()) await page.keyboard.press('Enter');
  await tabTo(password);
  await page.keyboard.type(user.password);
  await page.keyboard.press('Enter');
  await page.waitForURL(site + '/**');
  await page.getByRole('heading', {name:'Create a synthetic trip'}).waitFor();
  steps.push('Activate Sign in and complete synthetic dispatcher login using the keyboard.');
  await scan('dispatcher-create');
  // Set up a new synthetic record so the keyboard update never changes an existing demonstration trip.
  const id = await page.evaluate(async () => {
    const key = Object.keys(sessionStorage).find(name => name.startsWith('oidc.user:'));
    const catalog = await (await fetch('/api/catalog')).json();
    const response = await fetch('/api/trips', {method:'POST',
      headers:{Authorization:'Bearer ' + JSON.parse(sessionStorage.getItem(key)).access_token,'Content-Type':'application/json'},
      body:JSON.stringify({routeId:catalog.routes[0].id, vehicleId:catalog.vehicles[0].id,
        scheduledDeparture:new Date().toISOString()})});
    if (response.status !== 201) throw Error('Fixture setup failed: ' + response.status);
    return (await response.json()).id;
  });
  await activate(page.getByRole('button',{name:'View trip '+id,exact:true}));
  await scan('dispatcher-update');
  await tabTo(page.getByLabel('Total delay (minutes)'));
  await page.keyboard.press('ControlOrMeta+a');
  await page.keyboard.type('4');
  const updated = page.waitForResponse(r => r.url() === site+'/api/trips/'+id+'/updates' && r.request().method() === 'POST');
  await activate(page.getByRole('button',{name:'Save trip'}));
  expect((await updated).status()).toBe(200);
  const saved = await (await page.request.get(site+'/api/trips/'+id)).json();
  expect(saved.delayMinutes).toBe(4);
  expect(saved.version).toBe(2);
  steps.push('Open fresh synthetic trip '+id+', enter a four-minute delay, Tab to Save trip, press Enter. Public readback confirmed version 2 and delay 4.');
  await writeFile(output+'/keyboard.json',JSON.stringify({site,release,browser:browser.version(),steps},null,2)+'\n');
} finally {
  await browser.close();
}
