import assert from 'node:assert/strict';
import { chromium } from 'playwright';

const browser = await chromium.launch();
try {
  for (const host of ['staging.mzcampuspulse.com', 'routeboard.mzcampuspulse.com']) {
    const page = await browser.newPage();
    const response = await page.goto('https://' + host, { waitUntil: 'domcontentloaded' });
    assert.equal(response.status(), 200);
    const headers = await response.allHeaders();
    assert.match(headers['content-security-policy'], /script-src 'self'/);
    await page.getByRole('button', { name: 'Search trips' }).waitFor();
    console.log(host, response.status(), headers['content-security-policy'], 'React search control visible');
    await page.close();
  }
} finally {
  await browser.close();
}
