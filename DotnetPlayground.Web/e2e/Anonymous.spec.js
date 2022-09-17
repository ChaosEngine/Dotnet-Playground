// @ts-check
import { test, expect } from '@playwright/test';

test.describe('Logged in as Anonymous', () => {
	test.use({ storageState: './e2e/storageStates/Anonymous-storageState.json' });

	test('Homepage as Anonymous', async ({ browser }) => {
		const page = await browser.newPage({ ignoreHTTPSErrors: true });

		page.goto('InkBall/Home');

		// Expect a title "to contain" a substring.
		await expect(page).toHaveTitle(/Home - Dotnet Core Playground/);

		//Not logged-in message
		const welcome = page.locator('p.inkhome');
		await expect(welcome).toContainText(/You are not logged in ... or allowed 😅/);
	});

	test('Game page as Anonymous with redirect to LogIn', async ({ browser }) => {
		const page = await browser.newPage({ ignoreHTTPSErrors: true });

		page.goto('InkBall/Game');

		// Expects the URL to be Home because Game page redirect if no game present.
		await expect(page).toHaveURL(/.*Identity\/Account\/Login.*/);

		// Expect a title "to contain" a substring.
		await expect(page).toHaveTitle(/Log in - Dotnet Core Playground/);

		// Just login prompt
		const body= page.locator('body');
		await expect(body).toContainText(/Log in/);
	});
});
