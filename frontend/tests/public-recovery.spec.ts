import { expect, test, type Page } from '@playwright/test';

async function capture(page: Page, name: string) {
  await page.screenshot({ path: test.info().outputPath(name + '.png'), fullPage: true });
}

test('catalog, trips and synthetic positions recover independently', async ({ page }) => {
  let respond!: () => void;
  const pending = new Promise<void>(resolve => { respond = resolve; });
  let unavailable = true;
  const samples = [
    { path: '**/api/catalog', empty: { routes: [], stops: [], vehicles: [] } },
    { path: '**/api/trips?**', empty: { items: [], total: 0, page: 1, pageSize: 20 } },
    { path: '**/api/positions', empty: { source: 'Synthetic', items: [] } },
  ];
  for (const sample of samples) {
    await page.route(sample.path, async route => {
      await pending;
      await route.fulfill(unavailable ? { status: 503, json: { status: 503 } } : { json: sample.empty });
    });
  }
  await page.route('**/api/events', route => route.fulfill({ contentType: 'text/event-stream', body: ': heartbeat\n\n' }));
  await page.goto('/');
  for (const text of ['Loading route catalog…', 'Loading trips…', 'Loading synthetic positions…'])
    await expect(page.getByText(text, { exact: true })).toBeVisible();
  await capture(page, 'loading');
  respond();
  for (const message of ['The route catalog could not be loaded.', 'The trip board could not be loaded.', 'Synthetic positions unavailable.'])
    await expect(page.getByRole('alert').filter({ hasText: message })).toBeVisible();
  await capture(page, 'provider-failures');
  unavailable = false;
  await page.getByRole('button', { name: 'Retry catalog' }).click();
  await page.getByRole('button', { name: 'Retry', exact: true }).click();
  await expect(page.getByText('No routes are available in the catalog.')).toBeVisible();
  await expect(page.getByText('No trips match these filters.')).toBeVisible();
  await expect(page.getByText('No simulated positions yet.', { exact: false })).toBeVisible({ timeout: 12000 });
  await expect(page.getByRole('alert')).toHaveCount(0);
  await capture(page, 'recovered-empty');
});

test('the fixed timetable remains readable during a route estimate outage', async ({ page }) => {
  const scheduled = '2026-11-12T10:00:00Z';
  const trip = {
    id: 1, routeId: 1, routeName: 'Synthetic Loop', vehicleId: 1, vehicleCode: 'DEMO',
    status: 'scheduled', scheduledDeparture: scheduled, simulatedDeparture: null,
    delayMinutes: 0, version: 1, isSynthetic: true,
    stops: [{ stopId: 1, name: 'Demo North', order: 1, scheduled, estimated: scheduled, simulatedObserved: null }],
  };
  await page.route('**/api/events', route => route.fulfill({ contentType: 'text/event-stream', body: ': heartbeat\n\n' }));
  await page.route('**/api/catalog', route => route.fulfill({ json: {
    routes: [{ id: 1, code: 'LOOP', name: trip.routeName }], stops: [], vehicles: [],
  } }));
  await page.route('**/api/trips?**', route => route.fulfill({ json: { items: [trip], total: 1, page: 1, pageSize: 20 } }));
  await page.route('**/api/positions', route => route.fulfill({ json: { source: 'Synthetic', items: [] } }));
  let release!: () => void;
  const pending = new Promise<void>(resolve => { release = resolve; });
  await page.route('**/api/routes/1/estimate', async route => {
    await pending;
    await route.fulfill({ status: 503, json: { status: 503 } });
  });
  await page.goto('/');
  await page.getByRole('button', { name: 'View trip 1' }).click();
  await expect(page.getByText('Loading external route reference…')).toBeVisible();
  await capture(page, 'estimate-loading');
  release();
  await expect(page.getByText('External route reference could not be loaded.', { exact: false })).toBeVisible();
  await expect(page.getByText('Demo North', { exact: true })).toBeVisible();
  await capture(page, 'estimate-failure');
  await page.getByRole('button', { name: 'Close trip' }).click();
  await page.route('**/api/routes/1/estimate', route => route.fulfill({ json: {
    status: 'cached', seconds: 600, meters: 2000, retrievedAt: scheduled, source: 'Synthetic test reference',
  } }));
  await page.getByRole('button', { name: 'View trip 1' }).click();
  await expect(page.getByText('cached: 10 minutes', { exact: false })).toBeVisible();
  await capture(page, 'estimate-recovered');
});
