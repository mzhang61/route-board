import { chromium } from '@playwright/test';
import { readFile } from 'node:fs/promises';

const site = 'https://staging.mzcampuspulse.com';
const library = await readFile(new URL('../node_modules/web-vitals/dist/web-vitals.iife.js', import.meta.url), 'utf8');
const browser = await chromium.launch();
try {
  const samples = [];
  for (let run = 1; run <= 5; run++) {
    const context = await browser.newContext({ viewport: { width: 390, height: 844 } });
    const page = await context.newPage();
    const metrics = {};
    await page.exposeFunction('saveVital', (name, value) => { metrics[name] = value; });
    await page.addInitScript({content: library + `
      for (const observe of [webVitals.onLCP, webVitals.onCLS, webVitals.onINP]) {
        observe(metric => window.saveVital(metric.name, metric.value), {reportAllChanges: true});
      }`});
    const cdp = await context.newCDPSession(page);
    await cdp.send('Network.enable');
    await cdp.send('Network.setCacheDisabled', {cacheDisabled: true});
    await cdp.send('Network.emulateNetworkConditions', {
      offline: false, latency: 150, downloadThroughput: 200000, uploadThroughput: 90000
    });
    await cdp.send('Emulation.setCPUThrottlingRate', {rate: 4});
    await page.goto(site, {waitUntil: 'domcontentloaded'});
    await page.getByRole('button', {name: /View trip/}).first().waitFor();
    const release = await page.evaluate(async () => (await (await fetch('/release.txt')).text()).trim());
    await page.waitForTimeout(600);
    await page.getByRole('button', {name: 'Search trips'}).click();
    await page.getByRole('button', {name: /View trip/}).first().click();
    await page.getByRole('button', {name: 'Close trip'}).click();
    await page.waitForTimeout(800);
    await page.goto('about:blank');
    await page.waitForFunction(() => true);
    if (!['LCP','CLS','INP'].every(name => Number.isFinite(metrics[name]))) {
      throw Error('Missing metric in run ' + run + ': ' + JSON.stringify(metrics));
    }
    samples.push({run, release, ...metrics});
    await context.close();
  }
  console.log(JSON.stringify({site, browser: browser.version(), samples}, null, 2));
} finally {
  await browser.close();
}
