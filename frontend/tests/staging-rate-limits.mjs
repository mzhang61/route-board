// Run after a quiet one-minute window on staging. The drill submits only invalid writes.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { chromium } from '@playwright/test';

const site = 'https://staging.mzcampuspulse.com';
const identity = JSON.parse(await readFile('.local-secrets/oidc-smoke/dispatcher.json', 'utf8'));
const browser = await chromium.launch();
try {
  const page = await browser.newPage();
  const loginRedirect = page.waitForResponse(response => new URL(response.url()).pathname === '/api/auth/authorize');
  await page.goto(site);
  await page.getByRole('button', {name: 'Sign in', exact: true}).click();
  assert.equal((await loginRedirect).status(), 302);
  const email = page.locator('input[name=username]:visible, input[type=email]:visible').first();
  await email.fill(identity.email);
  const password = page.locator('input[type=password]:visible');
  if (!await password.count()) await page.getByRole('button', {name: 'Continue', exact: true}).click();
  await password.fill(identity.password);
  await page.getByRole('button', {name: 'Continue', exact: true}).click();
  await page.waitForURL(site + '/**');
  await page.waitForFunction(() => Object.keys(sessionStorage).some(key => key.startsWith('oidc.user:')));
  console.log('Browser sign-in used the application endpoint (302) and completed Auth0 login.');

  const result = await page.evaluate(async () => {
    const key = Object.keys(sessionStorage).find(name => name.startsWith('oidc.user:'));
    const token = JSON.parse(sessionStorage.getItem(key)).access_token;
    const codes = [];
    for (let i = 0; i < 31; i++) {
      const response = await fetch('/api/trips', {method:'POST',
        headers:{Authorization:'Bearer '+token,'Content-Type':'application/json'},
        body:JSON.stringify({routeId:0,vehicleId:0,scheduledDeparture:'invalid'})});
      codes.push(response.status);
    }
    return codes;
  });
  console.log('Dispatcher invalid write HTTP statuses', result.join(' '));
  assert.deepEqual(result, [...Array(30).fill(400), 429]);

  const catalog = await (await fetch(site+'/api/catalog')).json();
  const route = catalog.routes.find(record => record.code === 'RB-DEMO');
  for (const [name, path, expected] of [
    ['routing', '/api/routes/'+route.id+'/estimate', 200],
    ['login', '/api/auth/authorize', 302]
  ]) {
    const codes = [];
    for (let i = 0; i < 11; i++) {
      // Do not forward the drill's redirect requests to the identity provider.
      codes.push((await fetch(site+path,{redirect:'manual'})).status);
    }
    console.log(name, 'HTTP statuses', codes.join(' '));
    assert.ok(codes.includes(expected));
    assert.equal(codes.at(-1), 429);
    assert.ok(codes.every(code => code === expected || code === 429));
  }
  assert.equal((await fetch(site+'/api/catalog')).status, 200);
  console.log('Public catalog HTTP 200 after rate limits fired.');
} finally {
  await browser.close();
}
