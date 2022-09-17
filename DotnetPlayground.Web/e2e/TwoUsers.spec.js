// Import test with our new fixtures.
import { test, expect } from './TwoUsersFixtures';

async function testNoGameAllert(page) {
	// create a locator
	const span = page.locator('text=No active game for you');
	const divAlert = span.locator('..');

	// Expect an attribute "to be strictly equal" to the value.
	await expect(divAlert).toHaveAttribute('class', 'alert alert-dismissible fade show inkhome alert-success');

	const btnClose = divAlert.locator('button');
	await btnClose.click();
}

// Use adminPage and userPage fixtures in the test.
test('Playwright0 and Playwright1', async ({ Playwright0, Playwright1, Anonymous }) => {
	// ... interact with both Playwright0 and Playwright1 ...

	await Playwright0.page.goto('InkBall/Game');
	await expect(Playwright0.greeting).toHaveText('Welcome Playwright0');
	await testNoGameAllert(Playwright0.page);
	// await Playwright0.page.screenshot({ path: './e2e/screenshot0.png' });

	await Playwright1.page.goto('InkBall/Game');
	await expect(Playwright1.greeting).toHaveText('Welcome Playwright1');
	await testNoGameAllert(Playwright1.page);
	// await Playwright1.page.screenshot({ path: './e2e/screenshot1.png' });
});
