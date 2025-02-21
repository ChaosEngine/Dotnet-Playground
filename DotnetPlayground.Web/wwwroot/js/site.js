/*eslint-disable no-console*/
/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "clientValidate|handleAboutPageBranchHash" }]*/
/*global forge, bootstrap, i18next, i18nextBrowserLanguageDetector, i18nextHttpBackend, locI18next*/
"use strict";

var g_AppRootPath = location.pathname.match(/\/([^/]+)\//)[0],
	g_gitBranch = "GIT_BRANCH", g_gitHash = "GIT_HASH";

/**
 * Client side hash validation of clicked single hash row
 * @param {HTMLButtonElement} button triggering action
 */
function clientValidate(button) {
	const td = $(button).parent().parent().find("td");

	const key = td.eq(0).text();
	const orig_md5 = td.eq(1).text();
	const orig_sha = td.eq(2).text();

	if (orig_md5 === '' || orig_sha === '')
		return;

	let md = forge.md.md5.create();
	md.update(key);
	const md5 = md.digest().toHex();
	md = forge.md.sha256.create();
	md.update(key);
	const sha = md.digest().toHex();

	td.eq(1).css("color", (md5 === orig_md5 ? "green" : "red")).css('font-weight', 'bold');
	td.eq(2).css("color", (sha === orig_sha ? "green" : "red")).css('font-weight', 'bold');
}

/**
 * Client side hash validation of all hash rows
 */
function clientValidateAll() {
	$("button[value='Validate']").each((_index, item) => clientValidate(item));
}

/**
 * About page handling of various elements
 */
function handleAboutPageBranchHash() {
	let anchor = document.querySelector('#branchHash > a:first-child');
	if (anchor) {
		anchor.setAttribute('href', anchor.getAttribute('href') + g_gitBranch);
		const strong = anchor.querySelector('strong');
		if (strong)
			strong.innerText = g_gitBranch;
	}
	anchor = document.querySelector('#branchHash > a:last-child');
	if (anchor) {
		anchor.setAttribute('href', anchor.getAttribute('href') + g_gitHash);
		const strong = anchor.querySelector('strong');
		if (strong)
			strong.innerText = g_gitHash;
	}
}

/**
 * Custom alert bootstrap modal
 * @param {string} msg content shown
 * @param {string} title of the dialog
 * @param {Function} onCloseCallback callback executed on close
 */
function myAlert(msg = 'Content', title = 'Modal title', onCloseCallback = undefined) {
	const myModalEl = document.getElementById('divModal');
	const myModal = bootstrap.Modal.getOrCreateInstance(myModalEl, { keyboard: true, backdrop: true });

	if (onCloseCallback) {
		// on close action
		myModalEl.addEventListener('hidden.bs.modal', function listener(e) {
			// remove event listener
			e.target.removeEventListener(e.type, listener);

			// call handler with original context
			return onCloseCallback.call(this, e);
		});
	}

	myModalEl.querySelector('.modal-body').textContent = msg;
	document.getElementById('divModalLabel').textContent = title;
	myModal.show();
}

/**
 * Custom alert bootstrap modal - i18n version expecting translation keys
 * @param {string} msg content shown
 * @param {string} title of the dialog
 * @param {Function} onCloseCallback callback executed on close
function myAlertI18n(msg = 'common.dlgContent', title = 'common.dlgTitle', onCloseCallback = undefined) {
	const myModalEl = document.getElementById('divModal');
	const myModal = bootstrap.Modal.getOrCreateInstance(myModalEl, { keyboard: true, backdrop: true });

	if (onCloseCallback) {
		// on close action
		myModalEl.addEventListener('hidden.bs.modal', function listener(e) {
			// remove event listener
			e.target.removeEventListener(e.type, listener);

			// call handler with original context
			return onCloseCallback.call(this, e);
		});
	}

	// myModalEl.querySelector('.modal-body').textContent = msg;
	myModalEl.querySelector('.modal-body').dataset.i18n = msg;
	// document.getElementById('divModalLabel').textContent = title;
	document.getElementById('divModalLabel').dataset.i18n = title;
	window.localize("#divModal");
	myModal.show();
}
*/

/**
 * Global document ready function
 */
$(function () {

	function ajaxLog(level, message, url, line, col, error) {
		const logPath = g_AppRootPath + "Home/ClientsideLog";

		$.post(logPath, {
			level: level, message: message, url: url, line: line, col: col, error: error
		});
	}

	/**
	 * Enable/disable menu, dropdown links depending on login status
	 */
	function handleLogoutForm() {
		//if we're not seeing logoutForm form - disable secure/authorized links, otherwise enable registration
		const links2disable = document.getElementById("logoutForm") === null ?
			["aInkList", "aInkGame", "aInkGameHigh"] :
			["aInkRegister"];

		links2disable.forEach(id => {
			const el = document.getElementById(id);
			//el.removeAttribute("href");
			el.setAttribute("tabindex", "-1");
			el.setAttribute("aria-disabled", "true");
			el.classList.add("disabled");
		});
	}

	/**
	 * Registers service worker globally
	 * @param {string} rootPath is a path of all pages after FQDN name (ex. https://foo-bar.com/rootPath) or '/' if no root path
	 * @param {boolean} isDev indicates whether this is development (tru) or production (false) like environment
	 */
	function registerServiceWorker(rootPath, isDev) {
		if ('serviceWorker' in navigator
			//&& (navigator.serviceWorker.controller === null || navigator.serviceWorker.controller.state !== "activated")
		) {
			const version = encodeURIComponent(g_gitBranch + '_' + g_gitHash);
			const swUrl = `${rootPath}sw${(isDev === true ? '' : '.min')}.js?version=${version}`;

			navigator.serviceWorker
				.register(swUrl, { scope: rootPath })
				.then(() => console.log("Service Worker Registered"));

			navigator.serviceWorker
				.ready.then(() => console.log('Service Worker Ready'));
		}
	}

	function registerMyAlert(msg = 'Content', title = 'Modal title') {
		const divModal = document.createElement('div');
		divModal.id = "divModal";
		divModal.classList.add("modal");
		divModal.classList.add("fade");
		divModal.setAttribute("tabindex", "-1");
		divModal.setAttribute("aria-labelledby", "divModalLabel");
		divModal.setAttribute("aria-hidden", "true");
		divModal.innerHTML =
			'<div class="modal-dialog">' +
				'<div class="modal-content">' +
					'<div class="modal-header">' +
						`<h5 class="modal-title text-break" id="divModalLabel">${title}</h5>` +
						'<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>' +
					'</div>' +
					`<div class="modal-body text-break">${msg}</div>` +
					'<div class="modal-footer">' +
						'<button type="button" class="btn btn-primary" data-bs-dismiss="modal">Close</button>' +
					'</div>' +
				'</div>' +
			'</div>';
		document.body.appendChild(divModal);
	}

	function registerThemeChangeHandler() {
		//Taken from https://anduin.aiursoft.com/post/2020/3/27/bootstrap-dark-theme-minimum-style
		const initDarkTheme = function () {
			if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
				// dark mode
				$('html').attr("data-bs-theme", "dark");
			}
		};
		const initLightTheme = function () {
			if (window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches) {
				// light mode
				$('html').attr("data-bs-theme", "light");
			}
		};
		initDarkTheme();
		window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", initDarkTheme);
		window.matchMedia('(prefers-color-scheme: light)').addEventListener("change", initLightTheme);
	}

	function updateOnlineStatus() {
		const offlineIndicator = $("#offlineIndicator");

		if (offlineIndicator !== undefined) {
			const state = navigator.onLine ? $.t("common.online") : $.t("common.offline");
			offlineIndicator.html(state);
			offlineIndicator.show();
		}
	}

	function handleLocalization(isDev) {

		function renderLocalize() {
			window.localize('head');
			window.localize('body');

			// const img = $('#langDropdown button.nav-link > img');
			// img.attr('src', `${g_AppRootPath}images/flag_${lang}.svg`);
			// img.attr('alt', lang === 'en' ? translateFunc('nav.langDropdown.en') : translateFunc('nav.langDropdown.pl'));
		}

		// use plugins and options as needed, for options, detail see: http://i18next.com/docs/
		i18next
			// detect user language. learn more: https://github.com/i18next/i18next-browser-languageDetector
			.use(i18nextBrowserLanguageDetector)
			.use(i18nextHttpBackend)
			.init({
				debug: isDev,
				fallbackLng: false, // default language if nothing found by detector or disable loading fallback
				supportedLngs: ['en', 'pl'], // array of supported languages

				ns: ['translation', ...(location.pathname.match(/InkBall/) ? ['inkBall'] : '')],
				defaultNS: 'translation',

				backend: {
					loadPath: `${g_AppRootPath}locales/{{lng}}/{{ns}}${(isDev === true ? '' : '.min')}.json`
				}
			}, function (/* err, t */) {
				// for options see: https://github.com/i18next/jquery-i18next#initialize-the-plugin
				// jqueryI18next.init(i18next, $, { useOptionsAttr: true });

				const localize = locI18next.init(i18next, { useOptionsAttr: false });
				window.localize = localize;

				// start localizing, details: https://github.com/i18next/jquery-i18next#usage-of-selector-function
				renderLocalize();
			});

		// Language switcher
		$('#langDropdown button[data-lang]').on('click', function () {
			const lang = $(this).data('lang');

			i18next.changeLanguage(lang, function (/* err, t */) {
				// Update the content after language change
				renderLocalize();
			});
		});
	}


	/**
	 * Mapped after Microsoft.Extensions.Logging
	 */
	const logLevel = {
		Trace: 0,
		Debug: 1,
		Information: 2,
		Warning: 3,
		Critical: 5,
		Error: 4,
		None: 6
	};

	var org_trace = console.trace;
	var org_debug = console.debug;
	var org_info = console.info;
	var org_warn = console.warn;
	var org_error = console.error;

	console.trace = function (message) {
		ajaxLog(logLevel.Trace, message);
		org_trace.call(this, arguments);
	};

	console.debug = function () {
		//ajaxLog(logLevel.Debug, message);
		org_debug.call(this, arguments);
	};

	console.info = function (message) {
		ajaxLog(logLevel.Information, message);
		org_info.call(this, arguments);
	};

	console.warn = function (message) {
		ajaxLog(logLevel.Warning, message);
		org_warn.call(this, arguments);
	};

	console.error = function (msg, url, line, col, error) {
		ajaxLog(logLevel.Error, msg, url, line, col, error);
		org_error.call(this, arguments);
	};

	const isDevelopment = window.location.host.match(/:\d+/) !== null;
	registerServiceWorker(g_AppRootPath, isDevelopment);

	handleLogoutForm();

	window.addEventListener('online', updateOnlineStatus);
	window.addEventListener('offline', updateOnlineStatus);
	if (navigator.onLine === false)
		updateOnlineStatus();

	registerThemeChangeHandler();

	registerMyAlert();
	//overriding window.alert with own implementation
	window.alert = myAlert;
	// window.myAlertI18n = myAlertI18n;

	handleLocalization(isDevelopment);
});

window.onerror = function (msg, url, line, col, error) {
	// Note that col & error are new to the HTML 5 spec and may not be 
	// supported in every browser.  It worked for me in Chrome.
	//let extra = !col ? '' : ('\ncolumn: ' + col);
	//extra += !error ? '' : ('\nerror: ' + error);

	// You can view the information in an alert to see things working like this:
	//alert("Error: " + msg + "\nurl: " + url + "\nline: " + line + extra);
	console.error(msg, url, line, col, error);

	// If you return true, then error alerts (like in older versions of 
	// Internet Explorer) will be suppressed.
	return true;
};
