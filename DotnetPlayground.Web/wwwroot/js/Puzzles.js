/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "PuzzlesOnLoad" }]*/
/*global html2canvas*/
"use strict";

/**
 * Puzzles page onload event handler
 */
function PuzzlesOnLoad() {
	$('#customFile').on('change', function () {
		const file = $(this)[0].files[0].name;
		$(this).next('label').text(file);
	});

	$("input[type='radio'].form-check-input").on('change', function () {
		const img = $(this).next('label').find('img');
		const size = $('#rangeSize').val();

		const img_path = img.attr('src');
		$("#target").css("--bimg", "url(" + img_path + ")");

		const rotation = $("#rotation").val();
		$("#target").css("--trans", "scale(" + size * 0.01 + ") rotateZ(" + rotation + "deg)");
	})[0].focus();

	$("#rangeSize, #rotation").on('change', function () {
		const size = $("#rangeSize").val();
		let lbl = $("label[for='rangeSize']");

		if (window.localize) {
			lbl[0].dataset.i18nOptions = `{ 'size': ${size} }`;
			window.localize("label[for='rangeSize']");
		}
		else
			lbl.text(`Size ${size}`);

		const rotation = $("#rotation").val();
		lbl = $("label[for='rotation']");

		if (window.localize) {
			lbl[0].dataset.i18nOptions = `{ 'rotation': ${rotation} }`;
			window.localize("label[for='rotation']");
		}
		else
			lbl.text(`Rotation ${rotation}`);

		$("#target").css("--trans", "scale(" + size * 0.01 + ") rotateZ(" + rotation + "deg)");
	});

	$('input[type="file"]').on('change', function () {
		if (this.files && this.files[0]) {
			const img = document.createElement('img');
			const blob = URL.createObjectURL(this.files[0]);
			img.src = blob;

			img.onload = function () {
				const w = img.width;
				const h = img.height;

				const target = $("#target");
				target.css("--uploadedImg", "url(" + blob + ")");
				target.css("width", w);
				target.css("height", h);
				//URL.revokeObjectURL(this.src);
			};
		}
	});

	$(".puzzles form").on("submit", async (event) => {
		event.preventDefault();

		const target = $("#target")[0];
		const button = $(event.target).find("button[type='submit']")[0];
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
}
