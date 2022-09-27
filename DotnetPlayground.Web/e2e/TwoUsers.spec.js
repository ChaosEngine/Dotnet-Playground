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

async function testLoggedInGamesList(page) {
	await page.goto('InkBall/GamesList');

	const legend = page.locator('text=New game creation');
	await expect(legend).toBeVisible();

	const btnNewGame = page.locator('input[type=submit]', { hasText: 'New game' });
	await expect(btnNewGame).toHaveClass("btn btn-primary");
	await expect(btnNewGame).toBeVisible();
}

test('Playwright0 and Playwright1 - no games created', async ({ Playwright0, Playwright1, Anonymous }) => {
	// ... interact with Playwright0 and/or Playwright1 ...

	await testLoggedInAndNoGameAllert(Playwright0.page, Playwright0.userName);

	await testLoggedInAndNoGameAllert(Playwright1.page, Playwright1.userName);
});

test('Playwright0 and Playwright1 - GamesList', async ({ Playwright0, Playwright1 }) => {
	// ... interact with Playwright0 and/or Playwright1 ...

	await testLoggedInGamesList(Playwright0.page);
});

test('P0 create game, P1 joins', async ({ Playwright0, Playwright1 }) => {
	// ... interact with Playwright0 and/or Playwright1 ...

	await Playwright0.page.goto('InkBall/Home');

	const p0_GameType = Playwright0.page.locator('select#GameType');
	p0_GameType.selectOption({ label: 'First capture wins' });

	const p0_BoardSize = Playwright0.page.locator('select#BoardSize');
	p0_BoardSize.selectOption({ label: '20 x 26' });

	const p0_btnNewGame = Playwright0.page.locator('input[type=submit]', { hasText: 'New game' });
	await expect(p0_btnNewGame).toBeVisible();
	await p0_btnNewGame.click();


	await Playwright1.page.goto('InkBall/GamesList');

	const p1_Join = Playwright1.page.locator('input[type=submit]', { hasText: 'Join' });
	await expect(p1_Join).toBeVisible();
	await (p1_Join).click();

	const p0_btnCancel = Playwright0.page.locator('input[type=submit]#SurrenderButton');
	await expect(p0_btnCancel).toBeVisible();
	await p0_btnCancel.click();
});
