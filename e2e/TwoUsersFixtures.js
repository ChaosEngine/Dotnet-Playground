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
	// @type {import('@playwright/test').Page}
	page;
	// User name of the signed in user, e.g. "Playwright1".
	// @type {string}
	userName;

	/**
	 * Creates an instance of PlaywrightUser.
	 * @param {import('@playwright/test').Page} page for testing
	 * @param {string} userName user name of the logged on user
	 */
	constructor(page, userName) {
		this.page = page;
		this.userName = userName;
	}

	/**
	 * Creates and logs user to browser with possible previous browser storageState
	 * @param {import('@playwright/test').Browser} browser browser object
	 * @param {string} locale locale for the browser context
	 * @param {string} userName user name of logged on user
	 * @returns {PlaywrightUser} created user class
	 */
	static async create(browser, locale, userName) {
		const context = await browser.newContext({
			ignoreHTTPSErrors: true,
			storageState: `./e2e/storageStates/${userName}-storageState.json`,
			locale: locale
		});
		const page = await context.newPage({ locale: locale });

		return new PlaywrightUser(page, userName);
	}
}



// Extend base test by providing "Playwright1" and "Playwright2".
// This new "test" can be used in multiple test files, and each of them will get the fixtures.
export const test = base.extend(
	{
		/**
		 * Fixture for Playwright1 user. Creates and logs in user to browser with possible previous browser storageState.
		 * @param {{ browser: import('@playwright/test').Browser, locale: string }} param0 browser and locale from the test, use function to provide the created user to the test
		 * @param {function(PlaywrightUser): Promise<void>} use function to provide the created user to the test
		 * @returns {Promise<void>}
		 */
		Playwright1: async ({ browser }, use) => {
			await use(await PlaywrightUser.create(browser, use.locale, FixtureUsers[0].userName));
		},
		/**
		 * Fixture for Playwright2 user. Creates and logs in user to browser with possible previous browser storageState.
		 * @param {{ browser: import('@playwright/test').Browser, locale: string }} param0 browser and locale from the test, use function to provide the created user to the test
		 * @param {function(PlaywrightUser): Promise<void>} use function to provide the created user to the test
		 * @returns {Promise<void>}
		 */
		Playwright2: async ({ browser }, use) => {
			await use(await PlaywrightUser.create(browser, use.locale, FixtureUsers[1].userName));
		}
	}

	// Object.fromEntries(
	// 	Object.entries(FixtureUsers)
	// 		.map(([_key, val]) => [val.userName, async ({ browser }, use) => {
	// 			await use(await PlaywrightUser.create(browser, use.locale, val.userName));
	// 		}])
	// )
);
