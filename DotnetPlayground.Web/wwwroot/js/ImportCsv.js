/*global myAlert, i18next */

window.addEventListener('load', () => {

	let globalData = null;

	/**
	 * Convert CSV content to HTML table
	 * @see https://stackoverflow.com/a/1293163/1248177
	 * @see https://stackoverflow.com/a/1293163/1248177
	 * @param {string} csvContent CSV content as a string
	 * @param {string} delimiter Delimiter character
	 * @returns {HTMLTableElement} HTML table element
	 */
	const csvToTable = (csvContent, delimiter) => {
		// Remove \r and \n inside double-quoted values
		const csvData = csvContent.replace(/"(.*?)"/gs, (match) => {
			return match.replace(/[\r\n]/g, ''); // Removes only within quotes
		});

		const lines = csvData.split(/\r?\n/); // Handle different newline formats
		if (lines.length <= 0) return null; // bail if no lines

		const table = document.createElement('table');
		table.className = 'table table-bordered'; // Optional: Add Bootstrap styling
		let section = null;
		let rowCounter = 0;

		const rawData = [];

		for (const line of lines) {
			if (line.length === 0 || line.startsWith('#')) continue; // Skip empty lines

			const polished = line.split(new RegExp(`${delimiter}(?=(?:[^"]*"[^"]*")*[^"]*$)`)); // Handle delimiters inside quotes
			const values = polished.map(value => value.replace(/^"|"$/g, '').trim());

			if (!section) {
				section = table.createTHead();
				const row = section.insertRow();
				rawData[rowCounter] = [];

				for (let j = 0; j < values.length; j++) {
					const cell = document.createElement('th');
					const textual_content = values[j].trim();
					cell.textContent = textual_content;
					row.appendChild(cell);

					rawData[rowCounter][j] = textual_content;

				}
				section = table.createTBody();//switch to body
				rowCounter++;
				continue;//done with header, skip to next iteration
			}
			const row = section.insertRow();
			rawData[rowCounter] = [];

			for (let j = 0; j < values.length; j++) {
				const cell = row.insertCell();
				const textual_content = values[j].trim();
				cell.textContent = textual_content;

				rawData[rowCounter][j] = textual_content;
			}
			rowCounter++;
		}

		section = table.createCaption();
		section.dataset.i18n = 'importCsv.totalRows';
		section.dataset.i18nOptions = `{ 'count': ${rowCounter} }`;
		section.textContent = `Total rows: ${rowCounter}`;

		return { table, rawData };
	};

	document.getElementById('csvFile').addEventListener('change', (event) => {
		const file = event.target.files[0];

		if (file) {
			const reader = new FileReader();

			reader.onload = function (e) {
				const csv_file = e.target.result;
				const delimiter = document.getElementById('delimiter').value;
				const { table: table_element, rawData } = csvToTable(csv_file, delimiter || ',');
				globalData = rawData;
				if (table_element) {
					document.getElementById('tableContainer').innerHTML = ''; // Clear existing content
					document.getElementById('tableContainer').appendChild(table_element);

					if (window.localize)
						window.localize("#tableContainer");
				}
			};

			reader.readAsText(file);
		}
	});

	/**
	 * Submit the CSV data to the server
	 * @param {Array<Array<string>>} rawData 2D array of strings
	 * @returns {Promise<void>}
	 */
	async function submitToServerApproach(rawData) {
		const payload = JSON.stringify(rawData);
		// Required for AntiForgeryToken
		const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]').value;
		try {
			const response = await fetch('ImportCsv', {
				method: 'POST',
				body: payload,
				credentials: 'same-origin',
				cache: 'no-cache',
				redirect: 'follow',
				referrerPolicy: 'no-referrer',
				headers: {
					'Content-Type': 'application/json',
					'RequestVerificationToken': antiforgeryToken
				}
			});
			if (!response.ok) {
				const body = await response.text();
				if (typeof body === 'string' && body.indexOf('Error') !== -1) {
					// console.log(body);
					myAlert(i18next.t('importCsv.undErr'), i18next.t('importCsv.Failed'));
					return;
				}
				else
					myAlert(i18next.t('importCsv.badResp'), i18next.t('importCsv.Failed'));
				return;
			}
			//assuming json response
			const json = await response.json();

			myAlert(i18next.t('importCsv.goodResult', { rowsCount: json.rowsCount, nonEmptyCellsCount: json.nonEmptyCellsCount }), i18next.t('importCsv.success'));

		} catch (error) {
			myAlert(i18next.t('importCsv.exInterc'), i18next.t('importCsv.Failed'));
			// eslint-disable-next-line no-console
			console.error('Error:', error);
		}
	}

	/**
	 * Count non empty cells in the CSV data
	 * @param {Array<Array<string>>} rawData 2D array of strings
	 * @returns {Promise<void>}
	 */
	async function countClientSideApproach(rawData) {
		// rawData is a 2D array of strings
		// Each inner array represents a row of the CSV file
		// Each string represents a cell value

		//iterate over rows and cells of rawData
		const counter = rawData.reduce((row_accumulator, row) => {
			return row_accumulator + row.reduce((cell_acc, cell) => {
				return cell_acc + (cell && cell !== '' ? 1 : 0);
			}, 0);
		}, 0);


		myAlert(i18next.t('importCsv.goodResult', { rowsCount: rawData.length, nonEmptyCellsCount: counter }), i18next.t('importCsv.success'));
	}

	document.getElementById('csvForm').addEventListener('submit', async (event) => {
		event.preventDefault();

		//detect if form is valid
		const form = event.target;
		if (!form.checkValidity()) {
			form.reportValidity();
			return;
		}
		if (!globalData || globalData.length === 0) {
			myAlert(i18next.t('importCsv.noCsv'), i18next.t('importCsv.Failed'));
			return;
		}

		const buttonName = event.submitter ? event.submitter.name : null;
		switch (buttonName) {
			case 'btnSubmit':
				await submitToServerApproach(globalData);
				break;
			case 'btnCount':
				await countClientSideApproach(globalData);
				break;
			default:
				myAlert(i18next.t('importCsv.unknBtn'), i18next.t('importCsv.Failed'));
				break;
		}
	});

});
