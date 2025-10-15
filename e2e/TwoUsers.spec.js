// Import test with our new fixtures.
// import { test, expect } from './TwoUsersFixtures';
import { test, expect } from '@playwright/test';
import { FixtureUsers, PlaywrightUser } from './TwoUsersFixtures';
import { GameTestHelper } from './GameTestHelper';



//init
let Playwright1, Playwright2, helper = new GameTestHelper(expect);
test.beforeAll(async ({ browser }) => {
	Playwright1 = await PlaywrightUser.create(browser, FixtureUsers[0].userName);
	Playwright2 = await PlaywrightUser.create(browser, FixtureUsers[1].userName);
});


//////Tests//////
test('Playwright1 and Playwright2 - no games created', async () => {
	// ... interact with Playwright1 and/or Playwright2 ...

	await helper.testLoggedInAndNoGameAlert(Playwright1.page, Playwright1.userName);

	await helper.testLoggedInAndNoGameAlert(Playwright2.page, Playwright2.userName);
});

test('Playwright1 and Playwright2 - GamesList', async () => {
	// ... interact with Playwright1 and/or Playwright2 ...

	await helper.testLoggedInGamesList(Playwright1.page);
});	 

test('P1 create game, P2 joins, P2 wins', async () => {
	const p1 = Playwright1, p2 = Playwright2;
	// ... interact with Playwright1 and/or Playwright2 ...
	//create new game as p1
	await helper.createGameFromHome(p1);

	//P2 goes to game list and joins active game
	await helper.joinCreatedGame(p2, p1.userName);

	const randX = helper.getRandomInt(0, 8), randY = helper.getRandomInt(0, 19);
	await helper.delay(2 * 1000);//wait for signalR to settle in (?)
	await expect(p1.page.getByText(`Player ${Playwright2.userName} joining`)).toBeVisible();


	//put 5x p1 points and 5x p2 point interchangeably and verify existence
	await helper.putPointForPlayer(p1, randX + 11, randY + 3, p2);
	await helper.putPointForPlayer(p2, randX + 6, randY + 3, p1);
	await helper.putPointForPlayer(p1, randX + 12, randY + 4, p2);
	await helper.putPointForPlayer(p2, randX + 7, randY + 4, p1);
	await helper.putPointForPlayer(p1, randX + 11, randY + 5, p2);
	await helper.putPointForPlayer(p2, randX + 6, randY + 5, p1);
	await helper.putPointForPlayer(p1, randX + 10, randY + 4, p2);
	await helper.putPointForPlayer(p2, randX + 5, randY + 4, p1);
	await helper.putPointForPlayer(p1, randX + 6, randY + 4, p2);//center
	await helper.putPointForPlayer(p2, randX + 11, randY + 4, p1);//center



	await expect(p1.page.locator('circle')).toHaveCount(10 + 1);
	await expect(p2.page.locator('circle')).toHaveCount(10 + 1);

	await helper.chatPlayerToPlayer(p1, p2, 'hello man');
	await helper.chatPlayerToPlayer(p2, p1, 'yo yo yo!');

	//Ensure P1 sees P2 joined, then cancels started game
	// await gameTestHelper.surrenderOrCancelGame(p1);


	// await gameTestHelper.startDrawingLine(p1);
	// await gameTestHelper.putPointForPlayer(p1, randX + 11, randY + 3);
	// await gameTestHelper.putPointForPlayer(p1, randX + 12, randY + 4);
	// await gameTestHelper.putPointForPlayer(p1, randX + 11, randY + 5);
	// await gameTestHelper.putPointForPlayer(p1, randX + 10, randY + 4);
	// await gameTestHelper.putPointForPlayer(p1, randX + 11, randY + 3);
	// await gameTestHelper.verifyWin(p1, 'And the winner is... red.');
	// await gameTestHelper.verifyWin(p2, 'And the winner is... red.');


	await helper.startDrawingLine(p2);
	await helper.putPointForPlayer(p2, randX + 6, randY + 3);
	await helper.putPointForPlayer(p2, randX + 7, randY + 4);
	await helper.putPointForPlayer(p2, randX + 6, randY + 5);
	await helper.putPointForPlayer(p2, randX + 5, randY + 4);
	await helper.putPointForPlayer(p2, randX + 6, randY + 3);

	await helper.delay(4 * 1000);//wait for signalR to settle in (?)

	await helper.verifyWin(p1, 'And the winner is... blue.');
	await helper.verifyWin(p2, 'And the winner is... blue.');

});

test('AI game: put 5x4 points and AI surrounds it', async () => {
	const p1 = Playwright1;

	// ... interact as Playwright1 only ... create new AI game
	await helper.createGameFromHome(p1, 'Advantage of 5 paths wins', '40 x 52', true);

	await helper.delay(1 * 500);//wait for signalR to settle in (?)

	let randX = helper.getRandomInt(0, 6), randY = helper.getRandomInt(0, 8);
	await expect(p1.page.getByText(`Multi CPU Oponent UserPlayer`)).toBeVisible();



	const svg = await p1.page.locator('svg#screen');

	//put 4x p1 points and let AI surround it 5 times and win
	await helper.svgClick(svg, randX + 15, randY + 11);//1st point
	
	const firstOponentPoint = await svg.locator('circle[data-status="POINT_FREE_BLUE"]');//2nd point is AI
	const cy = await firstOponentPoint.getAttribute('cy');
	if (cy) {
		const cyInt = parseInt(cy);
		if (cyInt < 26)
			randY += 26;
	}


	await helper.svgClick(svg, randX + 16, randY + 11);
	await helper.svgClick(svg, randX + 15, randY + 12);
	await helper.svgClick(svg, randX + 16, randY + 12);

	await helper.svgClick(svg, randX + 20, randY + 11);
	await helper.svgClick(svg, randX + 21, randY + 11);
	await helper.svgClick(svg, randX + 20, randY + 12);
	await helper.svgClick(svg, randX + 21, randY + 12);

	await helper.svgClick(svg, randX + 25, randY + 11);
	await helper.svgClick(svg, randX + 26, randY + 11);
	await helper.svgClick(svg, randX + 25, randY + 12);
	await helper.svgClick(svg, randX + 26, randY + 12);

	await helper.svgClick(svg, randX + 15, randY + 16);
	await helper.svgClick(svg, randX + 16, randY + 16);
	await helper.svgClick(svg, randX + 15, randY + 17);
	await helper.svgClick(svg, randX + 16, randY + 17);

	await helper.svgClick(svg, randX + 20, randY + 16);
	await helper.svgClick(svg, randX + 21, randY + 16);
	await helper.svgClick(svg, randX + 20, randY + 17);
	await helper.svgClick(svg, randX + 21, randY + 17);

	//put dummy 2 spread points just to trigger AI to do its work
	for (let x = 2; x <= 38; x += 2)
		await helper.svgClick(svg, x, randY + 1);
	for (let x = 2; x <= 34; x += 2)
		await helper.svgClick(svg, x, randY + 3);

	await helper.delay(1 * 500);//wait for signalR to settle in (?)

	const winMessageVisible = await p1.page.locator('div.modal-body', { hasText: 'And the winner is... blue.'}).isVisible();
	if(!winMessageVisible)
	{
		await helper.svgClick(svg, 36, randY + 3);//somehow we are lacking last point, put it just to be sure
		// await helper.svgClick(svg, 38, randY + 3);//just to be sure
	}

	await helper.verifyWin(p1, 'And the winner is... blue.');
});