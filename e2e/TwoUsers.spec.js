// Import test with our new fixtures.
import { test, expect } from './TwoUsersFixtures';
// import { test, expect } from '@playwright/test';
// import { FixtureUsers, PlaywrightUser } from './TwoUsersFixtures';
import { GameTestHelper } from './GameTestHelper';



//init
const helper = new GameTestHelper(expect);

test.describe.configure({ mode: 'serial' });

//////Tests//////
test('Playwright1 and Playwright2 - no games created', async ({ Playwright1, Playwright2 }) => {
	// ... interact with Playwright1 and/or Playwright2 ...

	await helper.testLoggedInAndNoGameAlert(Playwright1.page, Playwright1.userName);

	await helper.testLoggedInAndNoGameAlert(Playwright2.page, Playwright2.userName);
});

test('Playwright1 and Playwright2 - GamesList', async ({ Playwright1/* , Playwright2  */ }) => {
	// ... interact with Playwright1 and/or Playwright2 ...

	await helper.testLoggedInGamesList(Playwright1.page);
});

test('P1 create game, P2 joins, P2 wins', async ({ Playwright1: p1, Playwright2: p2 }) => {
	// ... interact with Playwright1 and/or Playwright2 ...
	//create new game as p1
	await helper.createGameFromHome(p1);

	//P2 goes to game list and joins active game
	await helper.joinCreatedGame(p2, p1.userName);

	const randX = helper.getRandomInt(0, 8), randY = helper.getRandomInt(0, 19);
	// await helper.delay(2 * 1000);//wait for signalR to settle in (?)
	await expect(p1.page.getByText(`Player ${p2.userName} joining`)).toBeVisible();


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

	// await helper.delay(4 * 1000);//wait for signalR to settle in (?)
	await expect(p1.page.locator('polyline[data-id]')).toBeVisible();
	await expect(p2.page.locator('polyline[data-id]')).toBeVisible();

	const ownedCircles_p1 = await p1.page.locator(`circle[data-status="POINT_OWNED_BY_BLUE"]`).count();
	const ownedCircles_p2 = await p2.page.locator(`circle[data-status="POINT_OWNED_BY_BLUE"]`).count();
	expect(ownedCircles_p1).toBeGreaterThanOrEqual(1);
	expect(ownedCircles_p2).toBeGreaterThanOrEqual(1);

	await helper.verifyWin(p1, 'And the winner is... blue.');
	await helper.verifyWin(p2, 'And the winner is... blue.');

});

test.describe('AI tests', () => {
	// test.describe.configure({ mode: 'parallel' });

	test('Put 2x2 points and AI surrounds it', async ({ Playwright1: p1 }) => {

		// ... interact as Playwright1 only ... create new AI game
		await helper.createGameFromHome(p1, 'Advantage of 5 paths wins', '40 x 52', true);

		// await helper.delay(1 * 500);//wait for signalR to settle in (?)
		const multiCpuLabel = p1.page.locator('text=Multi CPU Oponent UserPlayer');
		await expect(multiCpuLabel).toBeVisible();
		await expect(multiCpuLabel).toHaveCSS('color', 'rgb(0, 0, 255)');//blue color for AI opponent

		//expect to have background file including 'FuturisticRobotProfile' name
		await expect(p1.page.locator('div.container.inkgame')).toHaveCSS('background-image', /FuturisticRobotProfile/);

		let randX = helper.getRandomInt(0, 6), randY = helper.getRandomInt(0, 8);



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


		await helper.svgClick(svg, randX + 15, randY + 12);
		await helper.svgClick(svg, randX + 16, randY + 12);

		await helper.svgClick(svg, randX + 20, randY + 12);
		await helper.svgClick(svg, randX + 21, randY + 12);

		await helper.svgClick(svg, randX + 25, randY + 12);
		await helper.svgClick(svg, randX + 26, randY + 12);

		await helper.svgClick(svg, randX + 15, randY + 17);
		await helper.svgClick(svg, randX + 16, randY + 17);

		await helper.svgClick(svg, randX + 20, randY + 17);
		await helper.svgClick(svg, randX + 21, randY + 17);

		//put dummy 2 spread points just to trigger AI to do its work
		for (let x = 0; x <= 38; x += 2) {
			if (x > 20 && await p1.page.locator('polyline[data-id][stroke="var(--bluish)"]').nth(4).isVisible())
				break;

			//check if point [x,randY + 0] is already taken, if not click it, otherwise click [x,randY + 4]
			if (await svg.locator(`circle[cx="${x}"][cy="${randY + 0}"][data-status="POINT_FREE_BLUE"]`).isVisible())
				await helper.svgClick(svg, x, randY + 4);
			else
				await helper.svgClick(svg, x, randY + 0);

			if (x > 21 && await p1.page.locator('polyline[data-id][stroke="var(--bluish)"]').nth(4).isVisible())
				break;

			//check if point [x,randY + 2] is already taken, if not click it, otherwise click [x,randY + 6]
			if (await svg.locator(`circle[cx="${x}"][cy="${randY + 2}"][data-status="POINT_FREE_BLUE"]`).isVisible())
				await helper.svgClick(svg, x, randY + 6);
			else
				await helper.svgClick(svg, x, randY + 2);
		}

		await expect(p1.page.locator('polyline[data-id][stroke="var(--bluish)"]').nth(4)).toBeVisible();

		await helper.verifyWin(p1, 'And the winner is... blue.');
	});


	test('Put C-like shape points and AI surrounds it', async ({ Playwright2: p2 }) => {

		// ... interact as Playwright1 only ... create new AI game
		await helper.createGameFromHome(p2, 'First capture wins', '40 x 52', true);

		// await helper.delay(1 * 500);//wait for signalR to settle in (?)
		const multiCpuLabel = p2.page.locator('text=Multi CPU Oponent UserPlayer');
		await expect(multiCpuLabel).toBeVisible();
		await expect(multiCpuLabel).toHaveCSS('color', 'rgb(0, 0, 255)');//blue color for AI opponent

		let randX = helper.getRandomInt(0, 6), randY = helper.getRandomInt(0, 8);



		const svg = await p2.page.locator('svg#screen');

		await helper.svgClick(svg, randX + 10, randY + 5);//1st point

		const firstOponentPoint = await svg.locator('circle[data-status="POINT_FREE_BLUE"]');//2nd point is AI
		const cy = await firstOponentPoint.getAttribute('cy');
		if (cy) {
			const cyInt = parseInt(cy);
			if (cyInt < 26)
				randY += 26;
		}

		let alreadyWon = false;
		for (let x = 10; x >= 5; x--) {
			if (await p2.page.locator('polyline[data-id][stroke="var(--bluish)"]').isVisible()) {
				alreadyWon = true;
				break;
			}
			await helper.svgClick(svg, randX + x, randY + 5);
		}
		if (!alreadyWon) {
			for (let y = 5; y <= 10; y++) {
				if (await p2.page.locator('polyline[data-id][stroke="var(--bluish)"]').isVisible()) {
					alreadyWon = true;
					break;
				}
				await helper.svgClick(svg, randX + 5, randY + y);
			}
		}
		if (!alreadyWon) {
			for (let x = 5; x <= 10; x++) {
				if (await p2.page.locator('polyline[data-id][stroke="var(--bluish)"]').isVisible()) {
					alreadyWon = true;
					break;
				}
				await helper.svgClick(svg, randX + x, randY + 10);
			}
		}

		//put dummy 2 spread points just to trigger AI to do its work
		if (!alreadyWon) {
			for (let x = 2; x <= 38; x += 2) {
				if (await p2.page.locator('polyline[data-id][stroke="var(--bluish)"]').isVisible())
					break;

				//check if point [x,randY + 1] is already taken, if not click it, otherwise click [x,randY + 3]
				if (await svg.locator(`circle[cx="${x}"][cy="${randY + 1}"][data-status="POINT_FREE_BLUE"]`).isVisible())
					await helper.svgClick(svg, x, randY + 3);
				else
					await helper.svgClick(svg, x, randY + 1);
			}
		}
		// await helper.delay(1 * 500);//wait for signalR to settle in (?)
		await expect(p2.page.locator('polyline[data-id][stroke="var(--bluish)"]')).toBeVisible();//expect 1 lines drawn by AI

		const ownedCircles = await svg.locator(`circle[data-status="POINT_OWNED_BY_BLUE"]`).count();
		expect(ownedCircles).toBeGreaterThanOrEqual(15);

		await helper.verifyWin(p2, 'And the winner is... blue.');
	});
});
