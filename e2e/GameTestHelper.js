
export class GameTestHelper {

	/**
	 * Creates an instance of GameTestHelper.
	 * @param {import('@playwright/test').Expect} expect expect object from test
	 */
	constructor(expect) {
		this.expect = expect;
	}

	/**
	 * tests that user is logged in and "no active game" alert is visible
	 * @param {import('@playwright/test').Page} page playwright page
	 * @param {string} userName str
	 */
	async testLoggedInAndNoGameAlert(page, userName) {
		await page.goto('InkBall/Game');

		// Example locator pointing to "Welcome User" greeting.
		const greeting = page.locator('p.inkhome');
		await this.expect(greeting).toHaveText(`Welcome ${userName}`);

		// create a locator
		const span = page.locator('text=No active game for you');
		const divAlert = span.locator('..');

		// Expect an attribute "to be strictly equal" to the value.
		await this.expect(divAlert).toHaveAttribute('class', 'alert alert-dismissible fade show inkhome alert-success');

		const btnClose = divAlert.locator('button');
		await btnClose.click();

		// await page.screenshot({ path: `./e2e/screenshot-${userName}.png` });
	}

	delay = ms => new Promise(resolve => setTimeout(resolve, ms));

	/**
	 * Gets random number in range: min(inclusive) - max (exclusive)
	 * @param {number} min - from(inclusive)
	 * @param {number} max - to (exclusive)
	 * @returns {number} random number
	 */
	getRandomInt = (min, max) => {
		min = Math.max(0, Math.min(min, max));
		max = Math.max(0, Math.max(max, min));
		min = Math.ceil(min);
		max = Math.floor(max);
		return Math.floor(Math.random() * (max - min)) + min; //The maximum is exclusive and the minimum is inclusive
	};

	testPointExistenceForPlayer = async (player, x, y) => {
		await this.expect(player.page.locator(`svg#screen > circle[cx="${x}"][cy="${y}"]`)).toBeVisible();
	};

	putPointForPlayer = async (player, x, y, otherPlayer = undefined) => {
		await player.page.locator('svg#screen').dblclick({ position: { x: x * 16, y: y * 16 } });
		// await player.page.locator('svg#screen').click({ position: { x: x * 16, y: y * 16 } });//two clicks make it somehow better?
		if (otherPlayer)
			await this.testPointExistenceForPlayer(otherPlayer, x, y);
	};

	svgClick = async (svgElement, x, y) => {
		// await svgElement.dblclick({ position: { x: x * 16, y: y * 16 } });
		await svgElement.click({ position: { x: x * 16, y: y * 16 }, delay: 50 });//two clicks make it somehow better?
		// this.delay(200);
	};

	
	/**
	 * tests that user is logged in and "no active game" alert is visible
	 * @param {import('@playwright/test').Page} page playwright page
	 */
	async testLoggedInGamesList(page) {
		await page.goto('InkBall/GamesList');

		const legend = page.locator('text=New game creation');
		await this.expect(legend).toBeVisible();

		const btnNewGame = page.locator('button[type=submit]', { hasText: 'New game' });
		await this.expect(btnNewGame).toHaveClass("btn btn-primary");
		await this.expect(btnNewGame).toBeVisible();
	}

	/**
	 * Creates new game from home page
	 * @param {import('@playwright/test').Page} player playwright page
	 * @param {string} gameTypeStr gateTypeStr
	 * @param {string} boardSizeStr boardSizeStr
	 * @param {boolean} ai if true, AI opponent is requested - 1 player game
	 */
	async createGameFromHome(player, gameTypeStr = 'First capture wins', boardSizeStr = '20 x 26', ai = false) {
		await player.page.goto('InkBall/Home');

		const selGameType = player.page.locator('select#GameType');
		selGameType.selectOption({ label: gameTypeStr });

		const selBoardSize = player.page.locator('select#BoardSize');
		selBoardSize.selectOption({ label: boardSizeStr });

		if (ai === true) {
			const selectAI = player.page.locator('input#CpuOponent');
			await selectAI.check();
		}

		//P1 creates new game and going into waiting-listening mode
		const btnNewGame = player.page.locator('button[type=submit]', { hasText: 'New game' });
		// await expect(btnNewGame).toBeVisible();
		await btnNewGame.click();

		await this.expect(player.page).toHaveURL(/.*InkBall\/Game/);

		await this.expect(player.page.locator('svg#screen')).toBeVisible();
	}

	/**
	 * Joins an existing game created by another player
	 * @param {import('@playwright/test').Page} player playwright page
	 * @param {string} otherPlayerNameWithGameToJoin name of the other player with the game to join
	 */
	async joinCreatedGame(player, otherPlayerNameWithGameToJoin = 'Playwright1') {
		await player.page.goto('InkBall/GamesList');

		const btnJoin = await player.page.locator('td.gtd', { hasText: otherPlayerNameWithGameToJoin })
			.locator('..')
			.locator('button[type=submit]', { hasText: 'Join' });

		// await expect(btnJoin).toBeVisible();
		await btnJoin.click();

		await this.expect(player.page).toHaveURL(/.*InkBall\/Game/);

		await this.expect(player.page.locator('svg#screen')).toBeVisible();
	}

	/**
	 * Surrenders or cancels the current game
	 * @param {import('@playwright/test').Page} player playwright page
	 */
	async surrenderOrCancelGame(player) {
		const btnCancel = player.page.locator('button[type=submit]#SurrenderButton');
		await this.expect(btnCancel).toBeVisible();

		await btnCancel.click();
	}

	/**
	 * Starts drawing a line on the game board
	 * @param {import('@playwright/test').Page} player playwright page
	 */
	async startDrawingLine(player) {
		const btnStopAndDraw = player.page.locator('button[type=button]#StopAndDraw');
		await this.expect(btnStopAndDraw).toBeVisible();

		await btnStopAndDraw.click();
	}

	/**
	 * Verifies that the player has won the game. Finally get back to GamesList page
	 * @param {import('@playwright/test').Page} player playwright page
	 * @param {string} message winning message
	 */
	async verifyWin(player, message = 'And the winner is... red.') {
		const legend1 = player.page.locator('div.modal-body', { hasText: message });
		await this.expect(legend1).toBeVisible();

		const btnModalClose1 = player.page.locator('button[data-bs-dismiss="modal"]', { hasText: 'Close' });
		await btnModalClose1.click();

		await this.expect(player.page).toHaveURL(/.*InkBall\/GamesList/);
	}

	/**
	 * Sends a chat message from one player to another
	 * @param {import('@playwright/test').Page} fromPlayer playwright page of the sender
	 * @param {import('@playwright/test').Page} toPlayer playwright page of the receiver
	 * @param {string} message chat message
	 */
	async chatPlayerToPlayer(fromPlayer, toPlayer, message) {
		await fromPlayer.page.locator('#messageInput').type(message);
		await fromPlayer.page.locator('button#sendButton').click();

		const p2_liMessagesList = toPlayer.page.locator('#messagesList', { hasText: message });
		await this.expect(p2_liMessagesList).toBeVisible();
	}
}
