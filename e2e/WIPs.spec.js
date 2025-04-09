// @ts-check
import { test, expect } from '@playwright/test';
import { Buffer } from 'buffer';


test.describe.configure({ mode: 'parallel' });

test.use({ storageState: './e2e/storageStates/Anonymous-storageState.json', ignoreHTTPSErrors: true });

test('ImportCsv - Open', async ({ browser }) => {
	const page = await browser.newPage();

	page.goto('ImportCsv');

	// Expect a title "to contain" a substring.
	await expect(page).toHaveTitle(/Import Csv - Dotnet Core Playground/);

	//Not logged-in message
	const welcome = page.locator('div.importCsv h3');
	await expect(welcome).toContainText(/Import CSV file, display it in a table and count non-empty cells./);
});


test('ImportCsv - Upload and Count', async ({ browser }) => {
	const page = await browser.newPage();

	page.goto('ImportCsv');

	// Locate the file input element
	const fileInput = page.locator('input[type="file"]'); // Example locator

	// Upload buffer from memory
	await fileInput.setInputFiles({
		name: 'file.txt',
		mimeType: 'text/plain',
		buffer: Buffer.from(
			'col1;col2;col3\n' +
			'1;2;\n' +
			'2;;6\n' +
			';;12\n' +
			'tekst1;TRUE;3.141592') // Example CSV content
	});


	// Locate the file input element
	const tableContainer = page.locator('#tableContainer'); // Example locator
	// Wait for the table to be visible
	await tableContainer.waitFor({ state: 'visible' });

	const delimiter = page.locator('#delimiter'); // Example locator
	//delimiter should be set to ';'
	await expect(delimiter).toHaveValue(';');

	//locate button btnCount and click it
	const btnCount = page.locator('button[type="submit"][name="btnCount"]'); // Example locator
	// Click the button
	await btnCount.click();
	//wait for modal to be visible
	const modal = page.locator('#divModal .modal-body'); // Example locator
	//verify modal content
	await modal.waitFor({ state: 'visible' });
	await expect(modal).toContainText(/Rows count: 5, non empty cells count: 11/);

});

test('Locale test - pl lang', async ({ browser }) => {
	const page = await browser.newPage({ locale: 'pl' });

	page.goto('ImportCsv');

	const flag_img = page.locator('#langDropdown > button > img');
	await expect(flag_img).toHaveAttribute('alt', 'Polski');

	//Not logged-in message
	const welcome = page.locator('div.importCsv h3');
	await expect(welcome).toContainText(/Importuj plik CSV, wyświetl go w tabeli i policz niepuste komórki./);
});
