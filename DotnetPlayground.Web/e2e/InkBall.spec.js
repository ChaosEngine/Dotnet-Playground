// @ts-check
import { test, expect } from '@playwright/test';

test('Homepage as Playwright0 with no game', async ({ page }) => {
	await page.goto('InkBall/Game');

	// Expect a title "to contain" a substring.
	await expect(page).toHaveTitle(/Home - Dotnet Core Playground/);

	// create a locator
	const span = page.locator('text=No active game for you');
	const divAlert = span.locator('..');

	// Expect an attribute "to be strictly equal" to the value.
	await expect(divAlert).toHaveAttribute('class', 'alert alert-dismissible fade show inkhome alert-success');

	const btnClose = divAlert.locator('button');
	await btnClose.click();

	// Expects the URL to contain intro.
	await expect(page).toHaveURL(/.*Home/);
});
