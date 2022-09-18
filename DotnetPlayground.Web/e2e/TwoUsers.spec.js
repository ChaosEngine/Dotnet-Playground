// Import test with our new fixtures.
import { test, expect } from './TwoUsersFixtures';

async function testLoggedInAndNoGameAllert(page, userName) {
	await page.goto('InkBall/Game');

	// Example locator pointing to "Welcome User" greeting.
	const greeting = page.locator('p.inkhome');
	await expect(greeting).toHaveText(`Welcome ${userName}`);

	// create a locator
	const span = page.locator('text=No active game for you');
	const divAlert = span.locator('..');

	// Expect an attribute "to be strictly equal" to the value.
	await expect(divAlert).toHaveAttribute('class', 'alert alert-dismissible fade show inkhome alert-success');

	const btnClose = divAlert.locator('button');
	await btnClose.click();

	// await page.screenshot({ path: `./e2e/screenshot-${userName}.png` });
}

// Use adminPage and userPage fixtures in the test.
test('Playwright0 and Playwright1 - no games created', async ({ Playwright0, Playwright1, Anonymous }) => {
	// ... interact with both Playwright0 and Playwright1 ...

	await testLoggedInAndNoGameAllert(Playwright0.page, Playwright0.userName);

	await testLoggedInAndNoGameAllert(Playwright1.page, Playwright1.userName);
});
