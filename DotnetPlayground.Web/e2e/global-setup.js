// global-setup.js
import { chromium, firefox, webkit, /* FullConfig  */ } from '@playwright/test';

async function globalSetup(config) {
  let browser = undefined;
  if (!browser && chromium)
    browser = await chromium.launch();
  if (!browser && firefox)
    browser = await firefox.launch();
  if (!browser && webkit)
    browser = await webkit.launch();
  const page = await browser.newPage({
    ignoreHTTPSErrors: true
  });

  const loginURL = config.projects[0].use.baseURL + 'Identity/Account/Login';
  await page.goto(loginURL);

  await page.locator('input[name="Input.Email"]').fill('Playwright0@test.domain.com');
  await page.locator('input[name="Input.Password"]').fill('Playwright0!');
  await page.locator('form#account button[type=submit]').click();

  // Save signed-in state to 'storageState.json'.
  await page.context().storageState({ path: './e2e/storageState.json' });

  await browser.close();
}

export default globalSetup;