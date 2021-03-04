/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "PuzzlesOnLoad" }]*/
/*eslint-disable no-console*/
"use strict";

function PuzzlesOnLoad() {
	$('#customFile').change(function () {
		const file = $(this)[0].files[0].name;
		$(this).next('label').text(file);
	});

	$("input[type='radio'].custom-control-input").change(function () {
		const img_path = $(this).next('label').find('img').attr('src');
		$('#target').css("background-image", "url(" + img_path + ")");
	});
}
