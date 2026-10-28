/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "PuzzlesOnLoad" }]*/
"use strict";

/**
 * Puzzles page onload event handler
 */
function PuzzlesOnLoad() {
	const target = $("#target");
	let uploadedImageUrl = "", imgPath = "";

	/**
	 * Loads an image asynchronously
	 * @param {string} src - The source URL of the image
	 * @returns {Promise<HTMLImageElement>} - A promise that resolves with the loaded image
	 */
	function loadImageAsync(src) {
		return new Promise((resolve, reject) => {
			const img = new Image();
			img.onload = () => resolve(img);
			img.onerror = () => reject(new Error(`Failed to load image: ${src}`));
			img.src = src;
		});
	}

	/**
	 * Renders the puzzle to a canvas element
	 * @param {HTMLElement} targetElement - The target element to render the puzzle on
	 * @param {string} selectedPattern - The source URL of the selected pattern image
	 * @returns {Promise<HTMLCanvasElement>} - A promise that resolves with the rendered canvas
	 */
	async function renderPuzzleToCanvas(targetElement, selectedPattern) {
		const width = Math.max(1, Math.round(targetElement.clientWidth));
		const height = Math.max(1, Math.round(targetElement.clientHeight));
		const rotation = Number($("#rotation").val()) || 0;
		const scale = (Number($("#rangeSize").val()) || 100) * 0.01;

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
			const px = 5 / 100;
			const patternSize = Math.max(1, width * 2 * px);
			const aspectRatio = patternImage.naturalWidth / Math.max(1, patternImage.naturalHeight);
			let tileWidth, tileHeight;

			if (aspectRatio >= 1) {
				tileWidth = patternSize * aspectRatio;
				tileHeight = patternSize;
			}
			else {
				tileWidth = patternSize;
				tileHeight = patternSize / aspectRatio;
			}

			const tileCanvas = document.createElement('canvas');
			tileCanvas.width = Math.max(1, Math.round(tileWidth));
			tileCanvas.height = Math.max(1, Math.round(tileHeight));
			const tileContext = tileCanvas.getContext('2d');
			if (tileContext) {
				tileContext.clearRect(0, 0, tileCanvas.width, tileCanvas.height);
				tileContext.drawImage(patternImage, 0, 0, tileCanvas.width, tileCanvas.height);
			}

			const pattern = context.createPattern(tileCanvas, 'repeat');
			if (pattern) {
				context.save();
				context.translate(width / 2, height / 2);
				context.rotate(rotation * Math.PI / 180);
				context.scale(scale, scale);
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

	$('#customFile').on('change', function (e) {
		const file = e.target.files[0].name;
		$(this).next('label').text(file);
	});

	$("input[type='radio'].form-check-input").on('change', function () {
		const img = $(this).next('label').find('img');
		const size = $('#rangeSize').val();

		imgPath = img.attr('src');
		target.css("--bimg", `url(${imgPath})`);

		const rotation = $("#rotation").val();
		target.css("--trans", `scale(${size * 0.01}) rotateZ(${rotation}deg)`);
	}).first().trigger('change');

	$("#rangeSize, #rotation").on('change', function () {
		const size = $("#rangeSize").val();
		let locator = "label[for='rangeSize']";
		let lbl = $(locator);

		if (window.localize) {
			lbl[0].dataset.i18nOptions = `{ 'size': ${size} }`;
			window.localize(locator);
		}
		else
			lbl.text(`Size ${size}`);

		const rotation = $("#rotation").val();
		locator = "label[for='rotation']";
		lbl = $(locator);

		if (window.localize) {
			lbl[0].dataset.i18nOptions = `{ 'rotation': ${rotation} }`;
			window.localize(locator);
		}
		else
			lbl.text(`Rotation ${rotation}`);

		target.css("--trans", `scale(${size * 0.01}) rotateZ(${rotation}deg)`);
	});

	$('input[type="file"]').on('change', function () {
		if (this.files && this.files[0]) {
			const img = document.createElement('img');
			const blob = URL.createObjectURL(this.files[0]);

			img.onload = function () {
				if (uploadedImageUrl)
					URL.revokeObjectURL(uploadedImageUrl);
				uploadedImageUrl = blob;
				const w = this.width, h = this.height;

				target.css("--uploadedImg", `url(${blob})`);
				target.css("width", `${w}px`);
				target.css("height", `${h}px`);
			};
			img.src = blob;
		}
	});

	$(".puzzles form").on("submit", async (e) => {
		e.preventDefault();

		const locator = ".puzzles button[type='submit']";
		const button = e.originalEvent.submitter;
		button.disabled = true;

		if (window.localize) {
			button.dataset.i18n = 'puzzles.saving';
			window.localize(locator);
		}
		else
			button.textContent = "Saving...";

		try {
			const canvas = await renderPuzzleToCanvas(target[0], imgPath);
			const link = document.createElement('a');
			link.download = 'puzzle.png';
			link.href = canvas.toDataURL('image/png');
			link.click();
		}
		finally {
			button.disabled = false;

			if (window.localize) {
				button.dataset.i18n = 'puzzles.save';
				window.localize(locator);
			}
			else
				button.textContent = "Save";
		}

	});
}
