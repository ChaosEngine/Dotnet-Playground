/* eslint-disable no-unused-vars */
// Import test with our new fixtures.
import { test, expect } from './TwoUsersFixtures';


//////handy helper functions - START//////
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

const delay = ms => new Promise(res => setTimeout(res, ms));

const testPointExistanceForPlayer = async (player, x, y) => {
	await expect(player.page.locator(`svg#screen > circle[cx="${x}"][cy="${y}"]`)).toBeVisible(/* { timeout: 20 * 1000 } */);
};

const putPointForPlayer = async (player, x, y, otherPlayer = undefined) => {
	await player.page.locator('svg#screen').click({ position: { x: x * 16, y: y * 16 }/* , timeout: 20 * 1000  */ });

	if (otherPlayer)
		await testPointExistanceForPlayer(otherPlayer, x, y);
};

async function testLoggedInGamesList(page) {
	await page.goto('InkBall/GamesList');

	const legend = page.locator('text=New game creation');
	await expect(legend).toBeVisible();

	const btnNewGame = page.locator('input[type=submit]', { hasText: 'New game' });
	await expect(btnNewGame).toHaveClass("btn btn-primary");
	await expect(btnNewGame).toBeVisible();
}

async function createGameFromHome(player, gameTypeStr = 'First capture wins', boardSizeStr = '20 x 26') {
	await player.page.goto('InkBall/Home');

	const selGameType = player.page.locator('select#GameType');
	selGameType.selectOption({ label: gameTypeStr });

	const selBoardSize = player.page.locator('select#BoardSize');
	selBoardSize.selectOption({ label: boardSizeStr });

	//P1 creates new game and goins into waiting-listening mode
	const btnNewGame = player.page.locator('input[type=submit]', { hasText: 'New game' });
	// await expect(btnNewGame).toBeVisible();
	await btnNewGame.click();

	await expect(player.page).toHaveURL(/.*InkBall\/Game/);

	await expect(player.page.locator('svg#screen')).toBeVisible();
}

async function joinCreatedGame(player, otherPlayerNameWithGameToJoin = 'Playwright1') {
	await player.page.goto('InkBall/GamesList');

	const btnJoin = await player.page.locator('td.gtd', { hasText: otherPlayerNameWithGameToJoin })
		.locator('..')
		.locator('input[type=submit]', { hasText: 'Join' });

	// await expect(btnJoin).toBeVisible();
	await btnJoin.click();

	await expect(player.page).toHaveURL(/.*InkBall\/Game/);

	await expect(player.page.locator('svg#screen')).toBeVisible();
}

async function surrenderOrCancelGame(player) {
	const btnCancel = player.page.locator('input[type=submit]#SurrenderButton');
	await expect(btnCancel).toBeVisible();

	await btnCancel.click();
}

async function startDrawingLine(player) {
	const btnStopAndDraw = player.page.locator('input[type=button]#StopAndDraw');
	await expect(btnStopAndDraw).toBeVisible();

	await btnStopAndDraw.click();
}

async function verifyWin(player, message = 'And the winner is... red.') {
	const legend1 = player.page.locator('div.modal-body', { hasText: message });
	await expect(legend1).toBeVisible();

	const btnModalClose1 = player.page.locator('button[data-bs-dismiss="modal"]', { hasText: 'Close' });
	await btnModalClose1.click();

	await expect(player.page).toHaveURL(/.*InkBall\/GamesList/);
}

async function chatPlayerToPlayer(fromPlayer, toPlayer, message) {
	await fromPlayer.page.locator('#messageInput').type(message);
	await fromPlayer.page.locator('input#sendButton').click();

	const p2_liMessagesList = toPlayer.page.locator('#messagesList', { hasText: message });
	await expect(p2_liMessagesList).toBeVisible();
}
//////handy helper functions - END//////




//////Tests//////
test('Playwright1 and Playwright2 - no games created', async ({ Playwright1, Playwright2 }) => {
	// ... interact with Playwright1 and/or Playwright2 ...

	await testLoggedInAndNoGameAllert(Playwright1.page, Playwright1.userName);

	await testLoggedInAndNoGameAllert(Playwright2.page, Playwright2.userName);
});

test('Playwright1 and Playwright2 - GamesList', async ({ Playwright1, Playwright2 }) => {
	// ... interact with Playwright1 and/or Playwright2 ...

	await testLoggedInGamesList(Playwright1.page);
});

test('P1 create game, P2 joins, P2 wins', async ({ Playwright1: p1, Playwright2: p2 }) => {
	// ... interact with Playwright1 and/or Playwright2 ...
	//create new game as p1
	await createGameFromHome(p1);

	//P2 goes to game list and joins active game
	await joinCreatedGame(p2, p1.userName);

	await delay(4 * 1000);//wait for signalR to settle in (?)

	//put 5x p1 points and 5x p2 point interchangebly and verify existence
	await putPointForPlayer(p1, 11, 3, p2);
	await putPointForPlayer(p2, 6, 3, p1);
	await putPointForPlayer(p1, 12, 4, p2);
	await putPointForPlayer(p2, 7, 4, p1);
	await putPointForPlayer(p1, 11, 5, p2);
	await putPointForPlayer(p2, 6, 5, p1);
	await putPointForPlayer(p1, 10, 4, p2);
	await putPointForPlayer(p2, 5, 4, p1);
	await putPointForPlayer(p1, 6, 4, p2);//center
	await putPointForPlayer(p2, 11, 4, p1);//center



	await expect(p1.page.locator('circle')).toHaveCount(10 + 1);
	await expect(p2.page.locator('circle')).toHaveCount(10 + 1);

	await chatPlayerToPlayer(p1, p2, 'hello man');
	await chatPlayerToPlayer(p2, p1, 'yo yo yo!');

	//Ensure P1 sees P2 joined, then cancells started game
	// await surrenderOrCancelGame(p1);


	// await startDrawingLine(p1);
	// await putPointForPlayer(p1, 11, 3);
	// await putPointForPlayer(p1, 12, 4);
	// await putPointForPlayer(p1, 11, 5);
	// await putPointForPlayer(p1, 10, 4);
	// await putPointForPlayer(p1, 11, 3);
	// await verifyWin(p1, 'And the winner is... red.');
	// await verifyWin(p2, 'And the winner is... red.');


	await startDrawingLine(p2);
	await putPointForPlayer(p2, 6, 3);
	await putPointForPlayer(p2, 7, 4);
	await putPointForPlayer(p2, 6, 5);
	await putPointForPlayer(p2, 5, 4);
	await putPointForPlayer(p2, 6, 3);
	await verifyWin(p1, 'And the winner is... blue.');
	await verifyWin(p2, 'And the winner is... blue.');

});
