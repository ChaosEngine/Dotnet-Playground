/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "HashesOnLoad" }]*/
"use strict";

/**
 * Hashes page onload event handler
 */
function HashesOnLoad() {

	let g_LastTimeOfRun = new Date().getTime();
	const divResult = document.getElementById('divResult');
	const txtSearch = document.getElementById('txtSearch');
	const btnSearch = document.getElementById('btnSearch');
	const resultTab = document.getElementById('result_tab');
	const form = document.getElementById('theForm');

	function getKind() {
		const kindEl = document.querySelector('.hash-kind input[type="radio"]:checked');
		return kindEl ? kindEl.value : '';
	}

	function getAntiForgeryToken() {
		const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
		return tokenInput ? tokenInput.value : '';
	}

	function renderValidationError(message) {
		if (divResult) {
			divResult.textContent = message;
		}
	}

	function isHashLengthValid(value, kind) {
		if (!kind) return false;
		if (kind === 'MD5') return value.length === 32;
		if (kind === 'SHA256') return value.length === 64;
		return false;
	}

	async function parseJsonOrText(response) {
		const contentType = response.headers.get("content-type") || "";
		if (contentType.includes("application/json")) {
			return response.json();
		}

		const text = await response.text();
		try {
			return JSON.parse(text);
		} catch (error) {
			void error;
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
		const search = txtSearch.value;
		if (search === null || search === '') {
			renderValidationError('no hash to decode');
			return;
		}

		const kind = getKind();
		switch (kind) {
			case 'MD5':
				if (search.length < 32) {
					renderValidationError('search.length < 32 characters, too short');
					return;
				}
				break;
			case 'SHA256':
				if (search.length < 64) {
					renderValidationError('search.length < 64 characters, too short');
					return;
				}
				break;
			default:
				renderValidationError('no hash method selected');
				return;
		}

		btnSearch.disabled = true;
		btnSearch.innerHTML = "<span class='spinner-border spinner-border-sm align-middle' role='status' aria-hidden='true'></span> Loading...";
		divResult.textContent = '';
		resultTab.style.display = 'none';

		const antiForgeryToken = getAntiForgeryToken();

		postForm('Search', antiForgeryToken, {
			"Search": search,
			"Kind": kind,
			"ajax": true
		}).then(function (found) {
			btnSearch.disabled = false;
			btnSearch.textContent = "Search";

			if (/^error.*/.test(found)) {
				divResult.textContent = found;
				return;
			}
			resultTab.style.display = '';

			const tbody = resultTab.querySelector('tbody');
			Array.from(tbody.querySelectorAll('tr')).forEach(function (tr) {
				if (tr.id !== 'trFirstResult') {
					tr.remove();
				}
			});
			document.getElementById('trFirstResult').style.display = '';

			document.getElementById('res_cel_key').textContent = found.key;
			document.getElementById('res_cel_md5').textContent = found.hashMD5;
			document.getElementById('res_cel_sha256').textContent = found.hashSHA256;
			const firstValidateCell = document.getElementById('res_cel_clientValidate');
			if (found.hashMD5 === null || found.hashSHA256 === null) {
				firstValidateCell.innerHTML = '';
			} else {
				firstValidateCell.innerHTML = '<button class="btn btn-success btn-sm" title="Validate" value="Validate" onclick="clientValidate(this)">Validate</button>';
			}
		}).catch(function (error) {
			btnSearch.disabled = false;
			btnSearch.textContent = "Search";
			divResult.textContent = "error: " + error.message;
		});
	}

	form.addEventListener('submit', function (event) {
		event.preventDefault();
		if (!isHashLengthValid(txtSearch.value, getKind())) {
			renderValidationError(txtSearch.dataset.valHashlength || 'Invalid hash length for selected algorithm.');
			return;
		}
		AjaxifySearch();
	});

	txtSearch.addEventListener("input", function () {
		//check if input was really changed from last time
		if (this.dataset.lastval !== this.value) {
			this.dataset.lastval = this.value;

			//change action
			const value = this.value;
			const time_of_run = new Date().getTime();

			//dont flood ajax reuqests, wait 1 sec in between
			if (value.length > 4 && ((time_of_run - g_LastTimeOfRun) > 1000)) {
				g_LastTimeOfRun = new Date().getTime();

				btnSearch.disabled = true;//simulate button click-like behaviour: disable
				btnSearch.innerHTML = "<span class='spinner-border spinner-border-sm align-middle' role='status' aria-hidden='true'></span> Loading...";

				const antiForgeryToken = getAntiForgeryToken();

				postForm('Autocomplete', antiForgeryToken, { "text": value, "ajax": true }).then(function (found) {
					resultTab.style.display = '';
					document.getElementById('trFirstResult').style.display = 'none';

					btnSearch.disabled = false;//simulate button click-like behaviour: enable
					btnSearch.textContent = "Search";

					const tbody = resultTab.querySelector('tbody');
					Array.from(tbody.querySelectorAll('tr')).forEach(function (tr) {
						if (tr.id !== 'trFirstResult') {
							tr.remove();
						}
					});

					(found || []).forEach(function (item) {
						const tr = document.createElement('tr');
						tr.innerHTML = `<td></td><td></td><td></td><td></td>`;
						tr.children[0].textContent = item.key;
						tr.children[1].textContent = item.hashMD5;
						tr.children[2].textContent = item.hashSHA256;
						if (item.hashMD5 === null || item.hashSHA256 === null) {
							tr.children[3].innerHTML = '';
						} else {
							tr.children[3].innerHTML = '<button class="btn btn-success btn-sm" title="Validate" value="Validate" onclick="clientValidate(this)">Validate</button>';
						}
						tbody.appendChild(tr);
					});
				}).catch(function () {
					btnSearch.disabled = false;//simulate button click-like behaviour: enable
					btnSearch.textContent = "Search";
				});
			}
		}
	});

	const spLastDate = document.getElementById("spLastDate");
	if (spLastDate) {
		//https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date/toLocaleString
		//https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl/DateTimeFormat/DateTimeFormat
		spLastDate.textContent = new Date(spLastDate.textContent).toLocaleString([], { dateStyle: 'medium', timeStyle: 'long' });
	}
}
