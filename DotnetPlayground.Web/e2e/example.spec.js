// @ts-check
import { test, expect } from '@playwright/test';

test('homepage has Playwright in title and get started link linking to the intro page', async ({ page }) => {
	await page.goto('https://localhost:4553/dotnet/InkBall/Game');

	// Expect a title "to contain" a substring.
	await expect(page).toHaveTitle(/Game - Dotnet Core Playground/);

	// create a locator
	const info = page.locator('text=This is Inball Game page');

	// Expect an attribute "to be strictly equal" to the value.
	await expect(info).toHaveAttribute('class', 'inkgame');

	const btnPause = page.locator('#Pause');

	await btnPause.click();

	// Expects the URL to contain intro.
	await expect(page).toHaveURL(/.*GamesList/);
});
