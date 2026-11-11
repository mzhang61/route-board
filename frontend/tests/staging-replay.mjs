import assert from 'node:assert/strict';
import http from 'node:http';
import net from 'node:net';
import { readFile } from 'node:fs/promises';
import { chromium, expect } from '@playwright/test';

const site = 'https://staging.mzcampuspulse.com';
const host = new URL(site).hostname;
const sockets = new Set();
let disconnected = false;
const tunnel = http.createServer();
tunnel.on('connect', (request, client, head) => {
  if (request.url !== host + ':443' || disconnected) return client.destroy();
  const remote = net.connect(443, host, () => {
    client.write('HTTP/1.1 200 Connection Established\r\n\r\n');
    if (head.length) remote.write(head);
    client.pipe(remote).pipe(client);
  });
  for (const socket of [client, remote]) {
    sockets.add(socket);
    socket.on('error', () => socket.destroy());
    socket.on('close', () => {
      sockets.delete(socket);
      client.destroy();
      remote.destroy();
    });
  }
});
await new Promise(resolve => tunnel.listen(0, '127.0.0.1', resolve));
const browser = await chromium.launch();
try {
  const dispatcher = await browser.newPage();
  const identity = JSON.parse(await readFile('.local-secrets/oidc-smoke/dispatcher.json', 'utf8'));
  await dispatcher.goto(site);
  await dispatcher.getByRole('button', { name: 'Sign in', exact: true }).click();
  await dispatcher.locator('input[name=username]:visible, input[type=email]:visible').first().fill(identity.email);
  const password = dispatcher.locator('input[type=password]:visible');
  if (!await password.count()) await dispatcher.getByRole('button', { name: 'Continue', exact: true }).click();
  await password.fill(identity.password);
  await dispatcher.getByRole('button', { name: 'Continue', exact: true }).click();
  await dispatcher.waitForURL(site + '/**');
  await dispatcher.waitForFunction(() => Object.keys(sessionStorage).some(key => key.startsWith('oidc.user:')));

  async function write(path, body, expectedStatus) {
    return dispatcher.evaluate(async ({path, body, expectedStatus}) => {
      const key = Object.keys(sessionStorage).find(key => key.startsWith('oidc.user:'));
      const token = JSON.parse(sessionStorage.getItem(key)).access_token;
      const response = await fetch(path, {
        method: 'POST', headers: { Authorization: 'Bearer ' + token, 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
      if (response.status !== expectedStatus) throw Error('Unexpected HTTP ' + response.status);
      return response.json();
    }, {path, body, expectedStatus});
  }
  const catalog = await dispatcher.evaluate(async () => (await fetch('/api/catalog')).json());
  const day = new Date().toISOString().slice(0, 10);
  const trip = await write('/api/trips', {
    routeId: catalog.routes.find(route => route.name === 'Synthetic River Loop').id,
    vehicleId: catalog.vehicles.find(vehicle => vehicle.code === 'RB-SYN-01').id,
    scheduledDeparture: day + 'T10:00:00Z'
  }, 201);
  assert.equal(new Date(trip.stops.at(-1).estimated).toISOString(), day + 'T10:23:00.000Z');
  const board = await browser.newPage({
    timezoneId: 'UTC', locale: 'en-US', proxy: { server: 'http://127.0.0.1:' + tunnel.address().port }
  });
  const network = await board.context().newCDPSession(board);
  await network.send('Network.enable');
  const events = [];
  const cursors = [];
  network.on('Network.eventSourceMessageReceived', message => {
    if (message.eventName !== 'trip') return;
    const snapshot = JSON.parse(message.data);
    if (snapshot.id === trip.id) events.push({ id: message.eventId, version: snapshot.version });
  });
  // Chromium includes the wire-level reconnect header in its extra-info event.
  network.on('Network.requestWillBeSentExtraInfo', ({headers}) => {
    const cursor = Object.entries(headers).find(([key]) => key.toLowerCase() === 'last-event-id');
    if (cursor) cursors.push(cursor[1]);
  });
  await board.goto(site, {waitUntil: 'domcontentloaded'});
  await expect.poll(() => events.length).toBe(1);
  disconnected = true;
  for (const socket of sockets) socket.destroy();
  await expect(board.getByText(/Live updates disconnected/)).toBeVisible();
  console.log('Public board disconnected at', JSON.stringify(events[0]), 'trip', trip.id);
  let version = trip.version;
  for (const delayMinutes of [2, 4]) {
    const result = await write('/api/trips/' + trip.id + '/updates', { version, kind: 'delay', delayMinutes }, 200);
    version = result.version;
    console.log('Dispatcher committed version', version, 'total delay', result.delayMinutes);
  }
  assert.equal(events.length, 1);
  disconnected = false;
  await expect.poll(() => events.length, {timeout: 30000}).toBe(3);
  assert.deepEqual(events.map(event => event.version), [1, 2, 3]);
  assert.equal(new Set(events.map(event => event.id)).size, 3);
  assert.ok(cursors.includes(events[0].id));
  const row = board.getByRole('row').filter({has: board.getByRole('button', {name: 'View trip ' + trip.id, exact: true})});
  await expect(row).toContainText('delayed');
  await row.getByRole('button').click();
  await expect(board.getByText(/Total recorded delay: 4 minutes. Viewing version 3/)).toBeVisible();
  await expect(board.locator('ol li').last()).toContainText(/Estimated: .*10:27:00 AM/);
  console.log('Reconnect cursors', JSON.stringify(cursors), 'events', JSON.stringify(events));
  console.log('Public detail verified: version 3, delay 4 minutes, final ETA 10:27 UTC.');
} finally {
  await browser.close();
  for (const socket of sockets) socket.destroy();
  tunnel.close();
}
