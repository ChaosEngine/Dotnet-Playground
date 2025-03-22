window.addEventListener('load', () => {

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

		for (const line of lines) {
			if (line.length === 0 || line.startsWith('#')) continue; // Skip empty lines

			const polished = line.split(new RegExp(`${delimiter}(?=(?:[^"]*"[^"]*")*[^"]*$)`)); // Handle delimiters inside quotes
			const values = polished.map(value => value.replace(/^"|"$/g, '').trim());

			if (!section) {
				section = table.createTHead();
				const row = section.insertRow();
				for (let j = 0; j < values.length; j++) {
					const cell = document.createElement('th');
					cell.textContent = values[j].trim();
					row.appendChild(cell);
				}
				section = table.createTBody();//switch to body
				continue;//done with header, skip to next iteration
			}
			const row = section.insertRow();
			for (let j = 0; j < values.length; j++) {
				const cell = row.insertCell();
				cell.textContent = values[j].trim();
			}
			rowCounter++;
		}

		section = table.createCaption();
		section.dataset.i18n = 'importCsv.totalRows';
		section.dataset.i18nOptions = `{ 'count': ${rowCounter} }`;
		section.textContent = `Total rows: ${rowCounter}`;

		return table;
	};

	document.getElementById('csvFile').addEventListener('change', (event) => {
		const file = event.target.files[0];

		if (file) {
			const reader = new FileReader();

			reader.onload = function (e) {
				const csvData = e.target.result;
				const delimiter = document.getElementById('delimiter').value;
				const table = csvToTable(csvData, delimiter || ',');
				if (table) {
					document.getElementById('tableContainer').innerHTML = ''; // Clear existing content
					document.getElementById('tableContainer').appendChild(table);

					if (window.localize)
						window.localize("#tableContainer");
				}
			};

			reader.readAsText(file);
		}
	});

});
