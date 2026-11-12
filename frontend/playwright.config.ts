import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './tests', use: { baseURL: 'http://127.0.0.1:4173' },
  webServer: { command: 'npm run dev -- --port 4173', url: 'http://127.0.0.1:4173', reuseExistingServer: false,
    env: {
      VITE_OIDC_AUTHORITY: 'http://127.0.0.1:4173/test-issuer',
      VITE_OIDC_CLIENT_ID: 'browser-test', VITE_OIDC_SCOPE: 'trips.write',
    } },
});
