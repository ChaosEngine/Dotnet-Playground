/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "PuzzlesOnLoad" }]*/
"use strict";

/**
 * Puzzles page onload event handler
 */
function PuzzlesOnLoad() {
	let uploadedImageUrl = "";

	function loadImageAsync(src) {
		return new Promise((resolve, reject) => {
			const img = new Image();
			img.onload = () => resolve(img);
			img.onerror = () => reject(new Error(`Failed to load image: ${src}`));
			img.src = src;
		});
	}

	async function renderPuzzleToCanvas(targetElement) {
		const width = Math.max(1, Math.round(targetElement.clientWidth));
		const height = Math.max(1, Math.round(targetElement.clientHeight));
		const rotation = Number($("#rotation").val()) || 0;
		const scale = (Number($("#rangeSize").val()) || 100) * 0.01;
		const selectedPattern = $("input[type='radio'].form-check-input:checked").next('label').find('img').attr('src') || '';

		const canvas = document.createElement('canvas');
		canvas.width = width;
		canvas.height = height;

		const context = canvas.getContext('2d');
		if (!context) {
			throw new Error('2D canvas context is not available');
		}

		if (uploadedImageUrl.length > 0) {
			const sourceImage = await loadImageAsync(uploadedImageUrl);
			context.drawImage(sourceImage, 0, 0, width, height);
		}

		if (selectedPattern.length > 0) {
			const patternImage = await loadImageAsync(selectedPattern);
			const pattern = context.createPattern(patternImage, 'repeat');
			if (pattern) {
				context.save();
				context.translate(width / 2, height / 2);
				context.scale(scale, scale);
				context.rotate(rotation * Math.PI / 180);
				context.fillStyle = pattern;
				context.fillRect(-width, -height, width * 2, height * 2);
				context.restore();
			}
		}

		const borderColor = getComputedStyle(targetElement).borderTopColor || 'rgb(55, 230, 192)';
		context.save();
		context.lineWidth = 4;
		context.strokeStyle = borderColor;
		context.strokeRect(2, 2, width - 4, height - 4);
		context.restore();

		return canvas;
	}

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
	}).first().trigger('change');

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
			uploadedImageUrl = blob;
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

		try {
			const canvas = await renderPuzzleToCanvas(target);
			const link = document.createElement('a');
			link.download = 'puzzle.png';
			link.href = canvas.toDataURL('image/png');
			link.click();
		}
		finally {
			button.disabled = false;

			if (window.localize) {
				button.dataset.i18n = 'puzzles.save';
				window.localize(".puzzles button[type='submit']");
			}
			else
				button.textContent = "Save";
		}

	});
}
