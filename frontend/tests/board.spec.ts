import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.beforeEach(async ({ page }) => {
  await page.route('**/api/positions', route => route.fulfill({ json: { source: 'Synthetic', items: [] } }));
  await page.route('**/api/events', route => route.fulfill({ contentType: 'text/event-stream', body: ': connected\n\n' }));
  await page.route('**/api/catalog', route => route.fulfill({ json: { routes: [], stops: [], vehicles: [] } }));
});

test('shows an empty board and has no serious accessibility violations', async ({ page }) => {
  await page.route('**/api/trips?**', route => route.fulfill({ json: { items: [], total: 0, page: 1, pageSize: 20 } }));
  await page.goto('/');
  await expect(page.getByText('No trips match these filters.')).toBeVisible();
  const result = await new AxeBuilder({ page }).analyze();
  expect(result.violations.filter(item => item.impact === 'serious' || item.impact === 'critical')).toEqual([]);
  await page.getByLabel('Search route or vehicle').focus();
  await page.keyboard.type('Synthetic');
  await page.keyboard.press('Enter');
  await expect(page.getByText('No trips match these filters.')).toBeVisible();
});

test('shows loading and recoverable error states', async ({ page }) => {
  let release: () => void = () => {};
  const pending = new Promise<void>(resolve => { release = resolve; });
  await page.route('**/api/trips?**', async route => { await pending; await route.fulfill({ status: 503, json: { status: 503 } }); });
  await page.goto('/');
  await expect(page.getByText('Loading trips…')).toBeVisible();
  release();
  await expect(page.getByRole('alert')).toContainText('could not be loaded');
  await expect(page.getByRole('button', { name: 'Retry' })).toBeEnabled();
});
