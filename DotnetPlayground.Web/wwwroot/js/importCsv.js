window.addEventListener('load', () => {

	const csvToTable = (csvContent) => {
		// Remove \r and \n inside double-quoted values
		const csvData = csvContent.replace(/"(.*?)"/gs, (match) => {
			return match.replace(/[\r\n]/g, ''); // Removes only within quotes
		});

		const lines = csvData.split(/\r?\n/); // Handle different newline formats
		if (lines.length <= 0) return null; // bail if no lines

		const table = document.createElement('table');
		table.className = 'table table-bordered'; // Optional: Add Bootstrap styling

		for (const line of lines) {
			if (line.length === 0 || line.startsWith('#')) continue; // Skip empty lines

			const polished = line.split(/,(?=(?:[^"]*"[^"]*")*[^"]*$)/); // Handle commas inside quotes
			const values = polished.map(value => value.replace(/^"|"$/g, '').trim());

			const row = table.insertRow();
			for (let j = 0; j < values.length; j++) {
				const cell = row.insertCell();
				cell.textContent = values[j];
			}
		}

		return table;
	};

	document.getElementById('csvFile').addEventListener('change', (event) => {
		const file = event.target.files[0];

		if (file) {
			const reader = new FileReader();

			reader.onload = function (e) {
				const csvData = e.target.result;
				const table = csvToTable(csvData);
				if (table) {
					document.getElementById('tableContainer').innerHTML = ''; // Clear existing content
					document.getElementById('tableContainer').appendChild(table);
				}
			};

			reader.readAsText(file);
		}
	});

});
