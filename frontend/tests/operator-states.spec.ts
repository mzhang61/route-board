import { expect, test, type Page } from '@playwright/test';

const issuer = 'http://127.0.0.1:4173/test-issuer';
const trip = {
  id: 8, routeId: 1, vehicleId: 1, routeName: 'Demo Loop', vehicleCode: 'SYN-1',
  status: 'scheduled', scheduledDeparture: '2026-11-12T10:00:00Z', simulatedDeparture: null,
  delayMinutes: 0, version: 1, isSynthetic: true, stops: [],
};

async function capture(page: Page, name: string) {
  await page.screenshot({ path: test.info().outputPath(name + '.png'), fullPage: true });
}

test.beforeEach(async ({ page }) => {
  await page.route('**/api/events', route => route.fulfill({ contentType: 'text/event-stream', body: ': heartbeat\n\n' }));
  await page.route('**/api/catalog', route => route.fulfill({ json: {
    routes: [{ id: 1, code: 'LOOP', name: 'Demo Loop' }], stops: [],
    vehicles: [{ id: 1, code: 'SYN-1', capacity: 20 }],
  } }));
  await page.route('**/api/trips?**', route => route.fulfill({ json: { items: [trip], page: 1, pageSize: 20, total: 1 } }));
  await page.route('**/api/positions', route => route.fulfill({ json: { source: 'Synthetic', items: [] } }));
  await page.route('**/api/routes/1/estimate', route => route.fulfill({ json: {
    status: 'unavailable', seconds: null, meters: null, retrievedAt: null, source: 'Test',
  } }));
});

test('sign-in can be retried after discovery fails', async ({ page }) => {
  let available = false;
  let release!: () => void;
  const pending = new Promise<void>(resolve => { release = resolve; });
  await page.route('**/.well-known/openid-configuration', async route => {
    await pending;
    await route.fulfill(available
      ? { json: { issuer, authorization_endpoint: issuer + '/authorize' } }
      : { status: 503, body: 'Unavailable' });
  });
  await page.route('**/authorize?**', route => route.fulfill({ contentType: 'text/html', body: '<h1>Test identity provider</h1>' }));
  await page.goto('/');
  const signIn = page.getByRole('button', { name: 'Sign in' });
  await signIn.click();
  await expect(page.getByText('Opening sign-in…')).toBeVisible();
  await capture(page, 'sign-in-opening');
  release();
  await expect(page.getByText('Could not open sign-in. Please retry.')).toBeVisible();
  await capture(page, 'sign-in-failed');
  available = true;
  await signIn.click();
  await expect(page.getByRole('heading', { name: 'Test identity provider' })).toBeVisible();
});

test('dispatch explains a forbidden creation and a conflicting update', async ({ page }) => {
  // The local browser fixture exercises UI handling. The server authorization suite tests real validation.
  await page.addInitScript(authority => {
    sessionStorage.setItem('oidc.user:' + authority + ':browser-test', JSON.stringify({
      access_token: 'browser-fixture', token_type: 'Bearer', expires_at: Date.now() / 1000 + 600, profile: { sub: 'synthetic' },
    }));
  }, issuer);
  await page.route('**/api/trips', route => route.fulfill({ status: 403, json: { status: 403 } }));
  let finish!: () => void;
  const pending = new Promise<void>(resolve => { finish = resolve; });
  await page.route('**/api/trips/8/updates', async route => {
    await pending;
    await route.fulfill({ status: 409, json: { status: 409 } });
  });
  await page.goto('/');
  await page.getByLabel('Departure (your local time)').fill('2026-11-12T10:00');
  await page.getByRole('button', { name: 'Save trip' }).click();
  await expect(page.getByText('Your account does not have dispatcher permission.')).toBeVisible();
  await capture(page, 'dispatch-forbidden');
  await page.getByRole('button', { name: 'View trip 8' }).click();
  await page.getByRole('button', { name: 'Save trip' }).click();
  await expect(page.getByRole('button', { name: 'Saving…' })).toBeDisabled();
  await capture(page, 'dispatch-saving');
  finish();
  await expect(page.getByText('The trip changed or this update is not allowed.', { exact: false })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Save trip' })).toBeEnabled();
  await capture(page, 'dispatch-conflict');
});

test('live status replaces the disconnect warning when the stream opens again', async ({ page }) => {
  // Drive the native event callbacks deterministically. Staging replay tests cover the actual transport.
  await page.addInitScript(() => {
    const NativeEventSource = window.EventSource;
    window.EventSource = class extends NativeEventSource {
      constructor(url: string | URL) {
        super(url);
        this.close();
        window.addEventListener('fixture-disconnect', () => this.dispatchEvent(new Event('error')));
        window.addEventListener('fixture-reconnect', () => this.dispatchEvent(new Event('open')));
      }
    };
  });
  await page.goto('/');
  await expect(page.getByText('Connecting to live updates…')).toBeVisible();
  await page.evaluate(() => window.dispatchEvent(new Event('fixture-disconnect')));
  await expect(page.getByText('Live updates disconnected.', { exact: false })).toBeVisible();
  await capture(page, 'live-disconnected');
  await page.evaluate(() => window.dispatchEvent(new Event('fixture-reconnect')));
  await expect(page.getByText('Live updates connected', { exact: true })).toBeVisible();
  await expect(page.getByText('Live updates disconnected.', { exact: false })).toHaveCount(0);
  await capture(page, 'live-reconnected');
});
