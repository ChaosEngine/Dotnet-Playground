/*global myAlert, i18next */

window.addEventListener('load', () => {

	let globalData = null;

	document.getElementById('delimiter')?.addEventListener('focus', function () {
		this.select();
	});

	/**
	 * Convert CSV content to HTML table
	 * @see https://stackoverflow.com/a/1293163/1248177
	 * @see https://stackoverflow.com/a/1293163/1248177
	 * @param {string} csvContent CSV content as a string
	 * @param {string} delimiterCharacter Delimiter character
	 * @returns {HTMLTableElement} HTML table element
	 */
	const csvToTable = (csvContent, delimiterCharacter) => {
		// Remove \r and \n inside double-quoted values
		const csvData = csvContent.replace(/"(.*?)"/gs, (match) => {
			return match.replace(/[\r\n]/g, ''); // Removes only within quotes
		});

		const lines = csvData.split(/\r?\n/); // Handle different newline formats
		if (lines.length <= 0) return null; // bail if no lines

		const table = document.createElement('table');
		table.className = 'table table-bordered caption-top'; // Optional: Add Bootstrap styling
		let section = null;
		let rowCounter = 0;

		const rawData = [];

		for (const line of lines) {
			if (line.length === 0 || line.startsWith('#')) continue; // Skip empty lines

			const polished = line.split(new RegExp(`${delimiterCharacter}(?=(?:[^"]*"[^"]*")*[^"]*$)`)); // Handle delimiters inside quotes
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
			}
			else {
				//add rows to body
				const row = section.insertRow();
				rawData[rowCounter] = [];

				for (let j = 0; j < values.length; j++) {
					const cell = row.insertCell();
					const textual_content = values[j].trim();
					cell.textContent = textual_content;

					rawData[rowCounter][j] = textual_content;
				}
			}
			rowCounter++;
		}

		section = table.createCaption();
		section.dataset.i18n = 'importCsv.totalRows';
		section.dataset.i18nOptions = `{ 'count': ${rowCounter} }`;
		section.textContent = `Total rows: ${rowCounter}`;

		return { table, rawData };
	};

	/**
	 * Handle file selection and processing
	 * @param {HTMLInputElement} file file to process
	 */
	const ProcessFile = (file) => {
		const reader = new FileReader();

		reader.onload = function (e) {
			const csv_file = e.target.result;

			let delimiter = document.getElementById('delimiter').value;
			if (delimiter === '' || delimiter.length === 0) {

				//if delimiter is empty, try to detect it
				//Count how many occurrences of "semicolon", "comma", "tab", "pipe", "colon" are in the file
				//and sort those character by count by most common
				const most_common_field_separator = [
					[';', (csv_file.match(/;/g) || []).length],
					[',', (csv_file.match(/,/g) || []).length],
					['\t', (csv_file.match(/\t/g) || []).length],
					['|', (csv_file.match(/\|/g) || []).length],
					[':', (csv_file.match(/:/g) || []).length]
				].sort(([, a_count], [, b_count]) => b_count - a_count);
				delimiter = most_common_field_separator[0][0];//get the most common delimiter character

				//set the delimiter in the input field
				document.getElementById('delimiter').value = delimiter;

			}
			const { table: table_element, rawData } = csvToTable(csv_file, delimiter);
			globalData = rawData;
			if (table_element) {
				const tableContainer = document.getElementById('tableContainer');
				tableContainer.replaceChildren(table_element); // Clear existing content and append new table
				//show the table container
				document.getElementById('tableContainer').classList.remove('d-none');

				if (window.localize)
					window.localize("#tableContainer");
			}
		};

		reader.readAsText(file);
	};

	const dropZone = document.getElementById('dropZone');
	const fileInput = document.getElementById('csvFile');

	const setDropZoneHighlight = (isHighlighted) => {
		dropZone.classList.toggle('drop-zone-highlight', isHighlighted);
	};

	const setFileInputDragState = (isDragging) => {
		fileInput.style.pointerEvents = isDragging ? 'none' : 'auto';
	};

	document.addEventListener('dragenter', () => {
		setFileInputDragState(true);
	});
	document.addEventListener('dragend', () => {
		setFileInputDragState(false);
	});
	document.addEventListener('drop', () => {
		setFileInputDragState(false);
	});

	// Unhighlight the drop zone when a file is dragged out of it.
	// Guard against nested children firing dragleave while the pointer is still within the zone,
	// which causes the highlight to flicker while the user drags over the drop area.
	const unhighlightDropZone = (event) => {
		const relatedTarget = event.relatedTarget;
		if (!relatedTarget || !dropZone.contains(relatedTarget)) {
			setDropZoneHighlight(false);
			setFileInputDragState(false);
		}
	};

	const handleFileDrop = (event) => {
		event.preventDefault();
		event.stopPropagation();
		setDropZoneHighlight(false);
		setFileInputDragState(false);

		if (event.dataTransfer.files.length > 0) {
			//get only 1st file
			const dt = new DataTransfer();
			dt.items.add(event.dataTransfer.files[0]);
			
			fileInput.files = dt.files;
			fileInput.dispatchEvent(new Event('change'));
		}
	};

	/**
	 * Handle file input change event
	 * @param {Event} event Event object
	 */
	fileInput.addEventListener('change', (event) => {
		const file = event.target.files[0];

		if (file)
			ProcessFile(file);
	});

	['dragover', 'dragenter'].forEach(eventType => {
		fileInput.addEventListener(eventType, (event) => {
			event.preventDefault();
			event.stopPropagation();
			setDropZoneHighlight(true);
		});
	});

	fileInput.addEventListener('dragleave', (event) => {
		event.preventDefault();
		event.stopPropagation();
		unhighlightDropZone(event);
	});

	fileInput.addEventListener('drop', handleFileDrop);

	/**
	 * Add dragover and dragenter event listeners to the drop zone; same event handler, showing the drop zone highlight
	 * @param {string} eventType Event type
	 */
	['dragover', 'dragenter'].forEach(eventType => {
		dropZone.addEventListener(eventType, (event) => {
			event.preventDefault();
			setFileInputDragState(true);
			setDropZoneHighlight(true);
		});
	});

	/**
	 * Remove highlight when leaving the drop zone
	 * @param {Event} event Event object
	 */
	dropZone.addEventListener('dragleave', (event) => {
		unhighlightDropZone(event); // Remove highlight only when leaving the whole drop zone
	});

	/**
	 * Handle file drop event
	 * @param {Event} event Event object
	 */
	dropZone.addEventListener('drop', handleFileDrop);

	/**
	 * Submit the CSV data to the server
	 * @param {Array<Array<string>>} rawData 2D array of strings
	 * @param {boolean} compression Whether to compress the payload
	 * @returns {Promise<void>}
	 */
	async function submitToServerApproach(rawData, compression = false) {
		try {
			const stringified = JSON.stringify(rawData);

			const headers = {
				'Content-Type': 'application/json',
				// Required for AntiForgeryToken
				'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
			};

			let payload, length_of_payload;
			//check if compression is needed
			if (compression) {
				const compressedStream = new CompressionStream('gzip');
				const compressedPayloadStream = new Blob([stringified]).stream().pipeThrough(compressedStream);
				const compressedPayload = await new Response(compressedPayloadStream).arrayBuffer();
				payload = compressedPayload;
				length_of_payload = compressedPayload.byteLength;
				headers['Content-Encoding'] = 'gzip';
			}
			else {
				payload = stringified;
				length_of_payload = stringified.length;
			}
			// eslint-disable-next-line no-console
			console.log('Payload size:', length_of_payload);


			const response = await fetch('ImportCsv', {
				method: 'POST',
				body: payload,
				credentials: 'same-origin',
				cache: 'no-cache',
				redirect: 'follow',
				referrerPolicy: 'no-referrer',
				headers: headers
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
	 * @returns {void}
	 */
	function countClientSideApproach(rawData) {
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


		const submitter = event.submitter;
		try {
			//detects which button was clicked
			const buttonName = submitter.name ? submitter.name : null;
			switch (buttonName) {
				case 'btnSubmit':
					{
						submitter.disabled = true;
						//get compress checkbox value
						const compress = document.getElementById('compression').checked;

						await submitToServerApproach(globalData, compress);
					}
					break;
				case 'btnCount':
					submitter.disabled = true;
					countClientSideApproach(globalData);
					break;
				default:
					myAlert(i18next.t('importCsv.unknBtn'), i18next.t('importCsv.Failed'));
					break;
			}
		} finally {
			submitter.disabled = false;
		}
	});

});
