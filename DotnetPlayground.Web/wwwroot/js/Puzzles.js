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
		// const one = Math.min(width, height);
		// console.log('width ' + width + ' ' + height);
		const img_path = img.attr('src');
		//$('.target').css("background-image", "url(" + img_path + ")");
		document.documentElement.style.setProperty("--bimg", "url(" + img_path + ")");
		//$('.target').css("background-size", size + "px " + size + "px");
		document.documentElement.style.setProperty("--size", size + "%");
		//$('.target').css("background-repeat", "repeat");
	})[0].focus();

	$("#rangeSize, #rotation").change(function () {
		const range = $("#rangeSize")[0];
		const size = range.value;
		let lbl = $("#rangeSize").prev('label');
		lbl.text('Size ' + size);

		const rotation = $("#rotation")[0];
		lbl = $("#rotation").prev('label');
		lbl.text('Rotation ' + rotation.value);

		//$('.target').css("background-size", size + "px " + size + "px");
		document.documentElement.style.setProperty("--size", size + "%");
		//$('.target').css("transform", "rotate(" + rotation.value + "deg)");
		document.documentElement.style.setProperty("--rot", "rotate(" + rotation.value + "deg)");
	});
}
