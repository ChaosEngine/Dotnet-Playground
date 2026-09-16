/* global clientValidateAll, clientValidate */
/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "HashesOnLoad" }]*/
"use strict";

/**
 * Hashes page onload event handler
 */
function HashesOnLoad() {

	let g_LastTimeOfRun = new Date().getTime();

	function setButtonLoading(button) {
		// 1. Create the spinner element
		const spinner = $('<span>', {
			class: 'spinner-border spinner-border-sm align-middle',
			role: 'status',
			'aria-hidden': 'true'
		});

		// 2. Disable button and update content safely
		button
			.prop('disabled', true)
			.empty()               // Clear old content safely
			.append(spinner)      // Append element
			.append(' Loading...'); // Append text node safely
	}

	function resetButton(button, originalText = 'Search') {
		button
			.prop('disabled', false)
			.text(originalText);   // .text() is completely safe from Trusted Types
	}

	function AjaxifySearch() {
		const divResult = $('#divResult');
		const search = $('#txtSearch').val();
		if (search === null || search === '') {
			divResult.text('no hash to decode');
			return false;
		}

		const kind = $('.hash-kind input[type="radio"]:checked').val();
		switch (kind) {
			case 'MD5':
				if (search.length < 32) {
					divResult.text('search.length < 32 characters, too short');
					return false;
				}
				break;
			case 'SHA256':
				if (search.length < 64) {
					divResult.text('search.length < 64 characters, too short');
					return false;
				}
				break;
			default:
				divResult.text('no hash method selected');
				return false;
		}

		const button = $('#btnSearch');
		button.prop('disabled', true);
		// Enable loading state:
		setButtonLoading(button);

		divResult.text('');
		$('#result_tab').hide();

		const hedrs = { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() };

		$.ajax({
			method: "POST", url: 'Search',
			headers: hedrs,
			data: {
				"Search": search, "Kind": kind, "ajax": true
			}
		}).done(function (found) {
			// Reset button state after AJAX call
			resetButton(button, 'Search');

			if (/^error.*/.test(found)) {
				divResult.text(found);
				return;
			}
			$('#result_tab').show();

			let t = $('#result_tab tbody');
			t.find('tr:visible').not('#trFirstResult').remove();
			$('#trFirstResult').show();

			$('#res_cel_key').text(found.key);
			$('#res_cel_md5').text(found.hashMD5);
			$('#res_cel_sha256').text(found.hashSHA256);
			$('#res_cel_clientValidate').empty().append((found.hashMD5 === null || found.hashSHA256 === null) ? null
				: $('<button>', {
					class: 'btn btn-success btn-sm js-client-validate',
					title: 'Validate',
					value: 'Validate',
					'aria-hidden': 'true'
				}).text('Validate')
			);
		});
		return false;
	}

	$.validator.addMethod('hashlength',
		function (value) {
			const kind = $('.hash-kind input[type="radio"]:checked').val();

			if (!kind || (kind === "MD5" && value.length !== 32) || (kind === "SHA256" && value.length !== 64))
				return false;

			return true;
		}, $('#txtSearch').data("val-hashlength")
	);

	$("#theForm").validate();
	$("#theForm").on('submit', AjaxifySearch);

	$("#txtSearch").on("input", function () {
		//check if input was really changed from last time
		if ($(this).data("lastval") !== $(this).val()) {
			$(this).data("lastval", $(this).val());

			//change action
			const value = $(this).val();
			const time_of_run = new Date().getTime();

			//dont flood ajax reuqests, wait 1 sec in between
			if (value.length > 4 && ((time_of_run - g_LastTimeOfRun) > 1000)) {
				g_LastTimeOfRun = new Date().getTime();

				const button = $('#btnSearch');//simulate button click-like behaviour: disable
				//Enable loading state:
				setButtonLoading(button);

				const hedrs = { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() };

				$.ajax({
					method: "POST", url: 'Autocomplete',
					headers: hedrs,
					data: { "text": value, "ajax": true }
				}).done(function (found) {
					$('#result_tab').show();
					$('#trFirstResult').hide();

					// Reset button state after AJAX call
					resetButton(button, 'Search');

					let t = $('#result_tab tbody');
					t.find('tr:visible').not('#trFirstResult').remove();

					$.each(found, function (i, item) {
						const valBtn = $('<button>', {
							class: 'btn btn-success btn-sm js-client-validate',
							title: 'Validate',
							value: 'Validate',
							'aria-hidden': 'true'
						}).text('Validate');

						$('<tr>').append(
							$('<td>').text(item.key),
							$('<td>').text(item.hashMD5),
							$('<td>').text(item.hashSHA256),
							$('<td>').append((item.hashMD5 === null || item.hashSHA256 === null) ? null : valBtn)
						).appendTo('#result_tab');
					});
				});
			}
		}
	});

	const spLastDate = $("#spLastDate");
	if (spLastDate.length > 0) {
		//https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date/toLocaleString
		//https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl/DateTimeFormat/DateTimeFormat
		spLastDate.text(new Date(spLastDate.text()).toLocaleString([], { dateStyle: 'medium', timeStyle: 'long' }));
	}

	$(document)
		.on('click', '.js-client-validate-all', clientValidateAll)
		.on('click', '.js-client-validate', function () {
			clientValidate(this);
		});
}
