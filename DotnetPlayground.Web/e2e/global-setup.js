// global-setup.js
import { chromium/* , FullConfig */ } from '@playwright/test';

async function globalSetup(config) {
  const browser = await chromium.launch();
  const page = await browser.newPage();

  await page.goto(config.projects.find(p => p.name === browser._name).use.baseURL + 'Identity/Account/Login');

  await page.locator('input[name="Input.Email"]').fill('Playwright0@test.domain.com');
  await page.locator('input[name="Input.Password"]').fill('Playwright0!');
  await page.locator('form#account button[type=submit]').click();

  // Save signed-in state to 'storageState.json'.
  await page.context().storageState({ path: './e2e/storageState.json' });

  await browser.close();
}

export default globalSetup;