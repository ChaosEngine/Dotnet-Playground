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

// const delay = ms => new Promise(res => setTimeout(res, ms));

async function testLoggedInGamesList(page) {
	await page.goto('InkBall/GamesList');

	const legend = page.locator('text=New game creation');
	await expect(legend).toBeVisible();

	const btnNewGame = page.locator('input[type=submit]', { hasText: 'New game' });
	await expect(btnNewGame).toHaveClass("btn btn-primary");
	await expect(btnNewGame).toBeVisible();
}

test('Playwright1 and Playwright2 - no games created', async ({ Playwright1, Playwright2, Anonymous }) => {
	// ... interact with Playwright1 and/or Playwright2 ...

	await testLoggedInAndNoGameAllert(Playwright1.page, Playwright1.userName);

	await testLoggedInAndNoGameAllert(Playwright2.page, Playwright2.userName);
});

test('Playwright1 and Playwright2 - GamesList', async ({ Playwright1, Playwright2 }) => {
	// ... interact with Playwright1 and/or Playwright2 ...

	await testLoggedInGamesList(Playwright1.page);
});

test('P0 create game, P1 joins', async ({ Playwright1: p1, Playwright2: p2 }) => {
	// ... interact with Playwright1 and/or Playwright2 ...

	await p1.page.goto('InkBall/Home');

	const p1_GameType = p1.page.locator('select#GameType');
	p1_GameType.selectOption({ label: 'First capture wins' });

	const p1_BoardSize = p1.page.locator('select#BoardSize');
	p1_BoardSize.selectOption({ label: '20 x 26' });

	//P1 creates new game and goins into waiting-listening mode
	const p1_btnNewGame = p1.page.locator('input[type=submit]', { hasText: 'New game' });
	await expect(p1_btnNewGame).toBeVisible();
	await p1_btnNewGame.click();
	await expect(p1.page).toHaveURL(/.*InkBall\/Game/);



	//P2 goes to game list and joins active game
	await p2.page.goto('InkBall/GamesList');
	const p2_Join = await p2.page.locator('td.gtd', { hasText: 'Playwright1' })
		.locator('..')
		.locator('input[type=submit]', { hasText: 'Join' });

	await expect(p2_Join).toBeVisible();
	await p2_Join.click();
	await expect(p2.page).toHaveURL(/.*InkBall\/Game/);




	await p1.page.locator('svg#screen').click({ position: { x: 1 * 16, y: 1 * 16 } });
	// await delay(3000);
	await p2.page.locator('svg#screen').click({ position: { x: 2 * 16, y: 2 * 16 } });
	// await delay(3000);


	await expect(p1.page.locator('svg#screen > circle')).toHaveCount(2 + 1);
	await expect(p2.page.locator('svg#screen > circle')).toHaveCount(2 + 1);


	//Ensure P1 sees P2 joined, then cancells started game
	const p1_btnCancel = p1.page.locator('input[type=submit]#SurrenderButton');
	await expect(p1_btnCancel).toBeVisible();

	await p1_btnCancel.click();
});
