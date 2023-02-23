// global-setup.js
import { chromium, firefox, webkit/*, FullConfig */ } from '@playwright/test';
import { FixtureUsers } from './TwoUsersFixtures';

async function signInUser(browser, loginURL, storageStatePath, user) {
	const page = await browser.newPage({ ignoreHTTPSErrors: true });

	if (user.email && user.password) {
		await page.goto(loginURL);

		await page.locator('input[name="Input.Email"]').fill(user.email);
		await page.locator('input[name="Input.Password"]').fill(user.password);
		await page.locator('form#account button[type=submit]').click();
	}

	// Save signed-in state to 'storageState.json'.
	await page.context().storageState({ path: `${storageStatePath}${user.userName}-storageState.json` });
}

async function globalSetup(config) {
	let browser = undefined;
	if (!browser && chromium)
		browser = await chromium.launch();
	if (!browser && firefox)
		browser = await firefox.launch();
	if (!browser && webkit)
		browser = await webkit.launch();

	const use = config.projects.find(p => p.name === browser._name).use;
	const loginURL = use.baseURL + 'Identity/Account/Login';

	for (const fu of FixtureUsers) {
		await signInUser(browser, loginURL, use.storageState, fu);
	}

	await browser.close();
}

export default globalSetup;