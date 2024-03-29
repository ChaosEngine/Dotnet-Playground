import { test as base/* , Page, Browser, Locator */ } from '@playwright/test';
export { expect } from '@playwright/test';

export const FixtureUsers = [
	{ userName: 'Playwright1', password: 'Playwright1!', email: 'Playwright1@test.domain.com' },	//Player1
	{ userName: 'Playwright2', password: 'Playwright2!', email: 'Playwright2@test.domain.com' }		//Player2
	, { userName: 'Anonymous' }																		//Anonymous user
];

// Page Object Model for the "PlaywrightUserX" page.
// Here you can add locators and helper methods specific to the admin page.
export class PlaywrightUser {
	// Page signed in as "PlaywrightX".

	constructor(page, userName) {
		this.page = page;
		this.userName = userName;
	}

	static async create(browser, userName) {
		const context = await browser.newContext({
			ignoreHTTPSErrors: true,
			storageState: `./e2e/storageStates/${userName}-storageState.json`
		});
		const page = await context.newPage();

		return new PlaywrightUser(page, userName);
	}
}

// Extend base test by providing "Playwright1" and "Playwright2".
// This new "test" can be used in multiple test files, and each of them will get the fixtures.
export const test = base.extend(
	{
		Playwright1: async ({ browser }, use) => {
			await use(await PlaywrightUser.create(browser, FixtureUsers[0].userName));
		},
		Playwright2: async ({ browser }, use) => {
			await use(await PlaywrightUser.create(browser, FixtureUsers[1].userName));
		}
	}
	
	// Object.fromEntries(
	// 	Object.entries(FixtureUsers)
	// 		.map(([/* key */, val]) => [val.userName, async ({ browser }, use) => {
	// 			await use(await PlaywrightUser.create(browser, val.userName));
	// 		}])
	// )
);
