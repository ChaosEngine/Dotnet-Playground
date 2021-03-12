/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "PuzzlesOnLoad" }]*/
/*eslint-disable no-console*/
"use strict";

function PuzzlesOnLoad() {
	$('#customFile').change(function () {
		const file = $(this)[0].files[0].name;
		$(this).next('label').text(file);
	});

	$("input[type='radio'].custom-control-input").change(function () {
		const img = $(this).next('label').find('img');
		const size = $('#rangeSize')[0].value;

		const img_path = img.attr('src');
		$(".target").css("--bimg", "url(" + img_path + ")");

		const rotation = $("#rotation")[0];
		$(".target").css("--trans", "scale(" + size * 0.01 + ") rotateZ(" + rotation.value + "deg)");
	})[0].focus();

	$("#rangeSize, #rotation").change(function () {
		const range = $("#rangeSize")[0];
		const size = range.value;
		let lbl = $("#rangeSize").prev('label');
		lbl.text('Size ' + size);

		const rotation = $("#rotation")[0];
		lbl = $("#rotation").prev('label');
		lbl.text('Rotation ' + rotation.value);

		$(".target").css("--trans", "scale(" + size * 0.01 + ") rotateZ(" + rotation.value + "deg)");
	});

	$(".puzzles form").on("submit", function (event) {
		event.preventDefault();
		console.log('submitados!');
	});
}
