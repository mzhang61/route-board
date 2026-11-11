import { chromium } from '@playwright/test';
import { readFile, writeFile } from 'node:fs/promises';

const site = 'https://staging.mzcampuspulse.com';
const issuer = 'https://dev-okl8jqszwoasg244.us.auth0.com';
const identity = JSON.parse(await readFile('.local-secrets/oidc-smoke/viewer.json', 'utf8'));
const browser = await chromium.launch();
try {
  const page = await browser.newPage();
  const entries = [];
  // Only selected public protocol fields enter the HAR. Credentials stay in the browser.
  page.on('response', response => {
    const url = new URL(response.url());
    if (![site, issuer].includes(url.origin)) return;
    if (!['/authorize', '/auth/callback', '/oauth/token'].includes(url.pathname)) return;
    const request = response.request();
    const timing = request.timing();
    const queryString = [...url.searchParams].map(([name, value]) => ({
      name, value: ['response_type', 'code_challenge_method', 'audience', 'scope'].includes(name) ? value : '[redacted]'
    }));
    entries.push({
      startedDateTime: new Date(timing.startTime).toISOString(), time: 0,
      request: { method: request.method(), url: url.origin + url.pathname, httpVersion: '',
        headers: [], queryString, cookies: [], headersSize: -1, bodySize: -1 },
      response: { status: response.status(), statusText: response.statusText(), httpVersion: '',
        headers: [], cookies: [], content: { size: 0, mimeType: '' }, redirectURL: '', headersSize: -1, bodySize: -1 },
      cache: {}, timings: { send: 0, wait: 0, receive: 0 },
      comment: 'Field-limited authentication trace. Bodies, headers, redirects and timing measurements omitted.'
    });
  });
  await page.goto(site);
  await page.getByRole('button', {name: 'Sign in', exact: true}).click();
  await page.locator('input[name=username]:visible, input[type=email]:visible').first().fill(identity.email);
  const password = page.locator('input[type=password]:visible');
  if (!await password.count()) await page.getByRole('button', {name: 'Continue', exact: true}).click();
  await password.fill(identity.password);
  await page.getByRole('button', {name: 'Continue', exact: true}).click();
  await page.waitForURL(site + '/**');
  await page.waitForFunction(() => Object.keys(sessionStorage).some(key => key.startsWith('oidc.user:')));
  for (const path of ['/authorize', '/auth/callback', '/oauth/token']) {
    if (!entries.some(entry => new URL(entry.request.url).pathname === path)) throw Error('Missing ' + path);
  }
  const har = {log: {version: '1.2', creator: {name: 'RouteBoard field-limited capture', version: '1'}, entries}};
  await writeFile('docs/evidence/staging-replay/login.redacted.har', JSON.stringify(har, null, 2) + '\n');
  console.log('Captured', entries.length, 'redacted protocol responses.');
} finally {
  await browser.close();
}
