// @ts-check
import { test, expect } from '@playwright/test';

/**
 * Tests redirects to authorize requiring pages
 * @param {import('@playwright/test').Page} page playwright page
 * @param {string} pageUrl url to page
 */
async function notAllowedAndRedirectToLogin(page, pageUrl) {
	await page.goto(pageUrl);

	// Expects the URL to be Home because Game page redirect if no game present.
	await expect(page).toHaveURL(/.*Identity\/Account\/Login.*/);

	// Expect a title "to contain" a substring.
	await expect(page).toHaveTitle(/Log in - Dotnet Core Playground/);

	// Just login prompt
	const body = page.locator('body');
	await expect(body).toContainText(/Log in/);
}

test.describe.configure({ mode: 'parallel' });

test.use({ storageState: './e2e/storageStates/Anonymous-storageState.json', ignoreHTTPSErrors: true });

test('Home as Anonymous', async ({ page }) => {

	await page.goto('InkBall/Home');

	// Expect a title "to contain" a substring.
	await expect(page).toHaveTitle(/InkBall Game - Dotnet Core Playground/);

	//Not logged-in message
	const welcome = page.locator('p.inkhome');
	await expect(welcome).toContainText(/You are not logged in ... or allowed 😅/);
});

test('Game page as Anonymous with redirect to LogIn', async ({ page }) => {
	await notAllowedAndRedirectToLogin(page, 'InkBall/Game');
});

test('GamesList page as Anonymous with redirect to LogIn', async ({ page }) => {
	await notAllowedAndRedirectToLogin(page, 'InkBall/GamesList');
});

test('Highscores page as Anonymous with redirect to LogIn', async ({ page }) => {
	await notAllowedAndRedirectToLogin(page, 'InkBall/Highscores');
});

/*
//No use in that really
test('NotExisting page as Anonymous with redirect to LogIn', async ({ page }) => {
	const resp = await page.goto('InkBall/NotExisting');

	// Expects the URL to be Home because Game page redirect if no game present.
	await expect(resp?.status()).toBe(404);
});
*/

test('Locale test - pl lang', async ({ browser }) => {
	const page = await browser.newPage({ locale: 'pl' });

	await page.goto('InkBall/Home');

	const flag_img = page.locator('#langDropdown > button > img');
	await expect(flag_img ).toHaveAttribute('alt', 'Polski');

	//Not logged-in message
	const welcome = page.locator('p.inkhome');
	await expect(welcome).toContainText(/Nie jesteś zalogoway ... albo uprawniony 😅/);
});
