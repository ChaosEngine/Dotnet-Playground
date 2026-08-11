/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "HashesOnLoad" }]*/
"use strict";

/**
 * Hashes page onload event handler
 */
function HashesOnLoad() {

	let g_LastTimeOfRun = new Date().getTime();

	async function parseJsonOrText(response) {
		const contentType = response.headers.get("content-type") || "";
		if (contentType.includes("application/json")) {
			return response.json();
		}

		const text = await response.text();
		try {
			return JSON.parse(text);
		} catch {
			return text;
		}
	}

	async function postForm(url, antiForgeryToken, dataObj) {
		const body = new URLSearchParams();
		Object.keys(dataObj).forEach(function (key) {
			body.append(key, dataObj[key]);
		});

		const response = await fetch(url, {
			method: "POST",
			headers: {
				"Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
				"RequestVerificationToken": antiForgeryToken
			},
			body: body.toString(),
			credentials: "same-origin"
		});

		if (!response.ok) {
			throw new Error(response.status + " " + response.statusText);
		}

		return parseJsonOrText(response);
	}

	function AjaxifySearch() {
		const divResult = $('#divResult');
		const search = $('#txtSearch').val();
		if (search === null || search === '') {
			divResult.text('no hash to decode');
			return;
		}

		const kind = $('.hash-kind input[type="radio"]:checked').val();
		switch (kind) {
			case 'MD5':
				if (search.length < 32) {
					divResult.text('search.length < 32 characters, too short');
					return;
				}
				break;
			case 'SHA256':
				if (search.length < 64) {
					divResult.text('search.length < 64 characters, too short');
					return;
				}
				break;
			default:
				divResult.text('no hash method selected');
				return;
		}

		const button = $('#btnSearch');
		button.prop('disabled', true);
		button.html("<span class='spinner-border spinner-border-sm align-middle' role='status' aria-hidden='true'></span> Loading...");
		divResult.text('');
		$('#result_tab').hide();

		const antiForgeryToken = $('input[name="__RequestVerificationToken"]').val();

		postForm('Search', antiForgeryToken, {
			"Search": search,
			"Kind": kind,
			"ajax": true
		}).then(function (found) {
			button.prop('disabled', false);
			button.text("Search");

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
			$('#res_cel_clientValidate').html((found.hashMD5 === null || found.hashSHA256 === null) ? ''
				: '<button class="btn btn-success btn-sm" title="Validate" value="Validate" onclick="clientValidate(this)">Validate</button>');
		}).catch(function (error) {
			button.prop('disabled', false);
			button.text("Search");
			divResult.text("error: " + error.message);
		});
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
	$("#theForm").data("validator").settings.submitHandler = function (form) {
		AjaxifySearch(form); return false;
		//alert('submitted'); return false;
	};

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
				button.prop('disabled', true);
				button.html("<span class='spinner-border spinner-border-sm align-middle' role='status' aria-hidden='true'></span> Loading...");

				const antiForgeryToken = $('input[name="__RequestVerificationToken"]').val();

				postForm('Autocomplete', antiForgeryToken, { "text": value, "ajax": true }).then(function (found) {
					$('#result_tab').show();
					$('#trFirstResult').hide();

					button.prop('disabled', false);//simulate button click-like behaviour: enable
					button.text("Search");

					let t = $('#result_tab tbody');
					t.find('tr:visible').not('#trFirstResult').remove();

					$.each(found, function (i, item) {
						$('<tr>').append(
							$('<td>').text(item.key),
							$('<td>').text(item.hashMD5),
							$('<td>').text(item.hashSHA256),
							$('<td>').html((item.hashMD5 === null || item.hashSHA256 === null) ? ''
								: '<button class="btn btn-success btn-sm" title="Validate" value="Validate" onclick="clientValidate(this)">Validate</button>')
						).appendTo('#result_tab');
						//console.log($tr.wrap('<p>').html());
					});
				}).catch(function (_error) {
					button.prop('disabled', false);//simulate button click-like behaviour: enable
					button.text("Search");
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
}
