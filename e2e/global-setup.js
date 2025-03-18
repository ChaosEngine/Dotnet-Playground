// global-setup.js
import fs from 'fs';
import { chromium, firefox, webkit/* , FullConfig */ } from '@playwright/test';
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

/**
 * Playwright global setup handler. Setup users, session/cookie stores and prepares
 * logged on browsers and anonymous browsers to tun tests
 * @param {import('@playwright/test').Config} config playwright config
 */
async function globalSetup(config) {
	let browser = undefined;
	const use = config.projects.at(0).use;
	const loginURL = use.baseURL + 'Identity/Account/Login';

	const now_date = new Date();

	for (const user of FixtureUsers) {
		const storageFile = `${use.storageState}${user.userName}-storageState.json`;
		let signInFreshUser = false;
		if (!fs.existsSync(storageFile)) {
			signInFreshUser = true;
		} else {
			const file_date = new Date(fs.statSync(storageFile).mtime);
			//
			// Taken from https://www.geeksforgeeks.org/how-to-calculate-the-number-of-days-between-two-dates-in-javascript/
			// Thanks
			// To calculate the time difference of two dates
			const Difference_In_Time = now_date.getTime() - file_date.getTime();

			// To calculate the no. of days between two dates
			const Difference_In_Days = Math.round(Difference_In_Time / (1000 * 3600 * 24));
			if (Difference_In_Days > 10) {
				// eslint-disable-next-line no-console
				console.log("Storage cookie created more than 10 days");
				signInFreshUser = true;
			}
		}

		if (signInFreshUser === true) {
			if (!browser && chromium)
				browser = await chromium.launch();
			if (!browser && firefox)
				browser = await firefox.launch();
			if (!browser && webkit)
				browser = await webkit.launch();

			await signInUser(browser, loginURL, use.storageState, user);
		}
	}

	if (browser)
		await browser.close();
}

export default globalSetup;