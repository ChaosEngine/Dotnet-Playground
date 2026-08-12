/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "PuzzlesOnLoad" }]*/
/*global html2canvas*/
"use strict";

/**
 * Puzzles page onload event handler
 */
function PuzzlesOnLoad() {
	const customFile = document.getElementById('customFile');
	const target = document.getElementById('target');
	const rangeSize = document.getElementById('rangeSize');
	const rotation = document.getElementById('rotation');
	const radioInputs = Array.from(document.querySelectorAll("input[type='radio'].form-check-input"));

	if (customFile) {
		customFile.addEventListener('change', function () {
			const file = this.files && this.files[0] ? this.files[0].name : '';
			const nextLabel = this.nextElementSibling;
			if (nextLabel)
				nextLabel.textContent = file;
		});
	}

	function updateTransform() {
		if (!target || !rangeSize || !rotation)
			return;
		target.style.setProperty("--trans", "scale(" + rangeSize.value * 0.01 + ") rotateZ(" + rotation.value + "deg)");
	}

	radioInputs.forEach(function (input) {
		input.addEventListener('change', function () {
			const label = this.nextElementSibling;
			const img = label ? label.querySelector('img') : null;
			if (img && target) {
				target.style.setProperty("--bimg", "url(" + img.getAttribute('src') + ")");
			}
			updateTransform();
		});
	});
	if (radioInputs.length > 0) {
		radioInputs[0].dispatchEvent(new Event('change'));
	}

	[rangeSize, rotation].forEach(function (el) {
		if (el) {
			el.addEventListener('change', function () {
				const size = rangeSize.value;
				let lbl = document.querySelector("label[for='rangeSize']");

				if (window.localize && lbl) {
					lbl.dataset.i18nOptions = `{ 'size': ${size} }`;
					window.localize("label[for='rangeSize']");
				}
				else if (lbl)
					lbl.textContent = `Size ${size}`;

				const rotValue = rotation.value;
				lbl = document.querySelector("label[for='rotation']");

				if (window.localize && lbl) {
					lbl.dataset.i18nOptions = `{ 'rotation': ${rotValue} }`;
					window.localize("label[for='rotation']");
				}
				else if (lbl)
					lbl.textContent = `Rotation ${rotValue}`;

				updateTransform();
			});
		}
	});

	Array.from(document.querySelectorAll('input[type="file"]')).forEach(function (fileInput) {
		fileInput.addEventListener('change', function () {
		if (this.files && this.files[0]) {
			const img = document.createElement('img');
			const blob = URL.createObjectURL(this.files[0]);
			img.src = blob;

			img.onload = function () {
				const w = img.width;
				const h = img.height;

				target.style.setProperty("--uploadedImg", "url(" + blob + ")");
				target.style.width = w + "px";
				target.style.height = h + "px";
				//URL.revokeObjectURL(this.src);
			};
		}
		});
	});

	Array.from(document.querySelectorAll('.puzzles form')).forEach(function (form) {
		form.addEventListener("submit", async (event) => {
		event.preventDefault();

		const button = event.target.querySelector("button[type='submit']");
		button.disabled = true;

		if (window.localize) {
			button.dataset.i18n = 'puzzles.saving';
			window.localize(".puzzles button[type='submit']");
		}
		else
			button.textContent = "Saving...";

		const canvas = await html2canvas(target);
		const link = document.createElement('a');
		link.download = 'puzzle.png';
		link.href = canvas.toDataURL();
		link.addEventListener('click', () => {
			button.disabled = false;

			if (window.localize) {
				button.dataset.i18n = 'puzzles.save';
				window.localize(".puzzles button[type='submit']");
			}
			else
				button.textContent = "Save";
		});
		link.click();

		});
	});
}
