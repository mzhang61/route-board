import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';
import { expect, test, type Page } from '@playwright/test';

async function capture(page: Page, name: string) {
  const directory = process.env.ROUTEBOARD_UI_EVIDENCE;
  if (!directory) return;
  await mkdir(directory, { recursive: true });
  await page.screenshot({ path: join(directory, name + '.png'), fullPage: true });
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
