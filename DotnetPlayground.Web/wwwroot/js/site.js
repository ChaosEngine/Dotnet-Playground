/*eslint-disable no-console*/
/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "clientValidate" }]*/
/*global forge*/
"use strict";

var g_AppRootPath = location.pathname.match(/\/([^/]+)\//)[0],
	g_LogPath = g_AppRootPath + "Home/ClientsideLog",
	g_IsDevelopment = window.location.host.match(/:\d+/) !== null;

function clientValidate(button) {
	const tr = $(button).parent().parent();

	const key = tr.find("td").eq(0).text();
	const orig_md5 = tr.find("td").eq(1).text();
	const orig_sha = tr.find("td").eq(2).text();

	if (orig_md5 === '' || orig_sha === '')
		return;

	let md = forge.md.md5.create();
	md.update(key);
	const md5 = md.digest().toHex();
	md = forge.md.sha256.create();
	md.update(key);
	const sha = md.digest().toHex();

	tr.find("td").eq(1).html("<strong style='color:" + (md5 === orig_md5 ? "green" : "red") + "'>" + orig_md5 + "</strong>");
	tr.find("td").eq(2).html("<strong style='color:" + (sha === orig_sha ? "green" : "red") + "'>" + orig_sha + "</strong>");
}

/**
 * Global document ready function
 */
$(function () {

	function ajaxLog(level, message, url, line, col, error) {
		$.post(g_LogPath, {
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

		links2disable.forEach(function (id) {
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
			const swUrl = rootPath + 'sw' + (isDev === true ? '' : '.min') + '.js?domain=' + encodeURIComponent(rootPath) + '&isDev=' + encodeURIComponent(isDev);

			navigator.serviceWorker
				.register(swUrl, { scope: rootPath })
				.then(function () {
					console.log("Service Worker Registered");
				});

			navigator.serviceWorker
				.ready.then(function () {
					console.log('Service Worker Ready');
				});
		}
	}

	function updateOnlineStatus() {
		const offlineIndicator = $("#offlineIndicator");

		if (offlineIndicator !== undefined) {
			const state = navigator.onLine ? "Online" : "Offline";
			offlineIndicator.html(state);
			offlineIndicator.show();
		}
	}


	/**
	 * Mapped after Microsoft.Extensions.Logging
	 * */
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

	registerServiceWorker(g_AppRootPath, g_IsDevelopment);

	handleLogoutForm();

	window.addEventListener('online', updateOnlineStatus);
	window.addEventListener('offline', updateOnlineStatus);
	if (navigator.onLine === false)
		updateOnlineStatus();




	//Taken from https://anduin.aiursoft.com/post/2020/3/27/bootstrap-dark-theme-minimum-style
	const initDarkTheme = function () {
		if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
			// dark mode
			$('.navbar-light').addClass('navbar-dark');
			$('.navbar-light').removeClass('navbar-light');
			$('body').addClass('bg-dark');
			$('body').css('color', 'white');
			$('.modal-content').addClass('bg-dark');
			$('.modal-content').css('color', 'white');
			$('.container-fluid').addClass('bg-dark');
			$('.container-fluid').css('color', 'white');
			$('.form-control').css('color', 'white');
			$('.form-control').css('background-color', 'rgb(33, 37, 41)');
			$('.form-select').addClass('bg-dark text-white');
			$('.form-select').removeClass('bg-light text-black');
			$('.list-group-item').addClass('bg-dark');
			$('.list-group-item').css('color', 'white');
			$('.content-wrapper').addClass('bg-dark');
			$('.card').addClass('bg-dark');
			$('.card-body').css('border', '1px solid rgba(255,255,255,.125)');
			$('.bg-light').addClass('bg-dark');
			$('.bg-light').removeClass('bg-light');
			$('.bg-white').addClass('bg-dark');
			$('.bg-white').removeClass('bg-white');
			$('.bd-footer').addClass('bg-dark');
			$('table').addClass('table-dark');
			$('table').removeClass('table-light');
			$('#editor.ace_editor').addClass('ace-chaos ace_dark');
			$('#editor.ace_editor').removeClass('ace-tm');
		}
	};
	const initLightTheme = function () {
		if (window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches) {
			// light mode
			// $('.navbar-dark').addClass('navbar-light');
			// $('.navbar-dark').removeClass('navbar-dark');
			$('body').removeClass('bg-dark');
			$('body').css('color', 'black');
			$('.modal-content').addClass('bg-light');
			$('.modal-content').css('color', 'rgb(33, 37, 41)');
			$('.container-fluid').addClass('bg-light');
			$('.container-fluid').css('color', 'rgb(33, 37, 41)');
			$('.form-control').css('color', 'rgb(33, 37, 41)');
			$('.form-control').css('background-color', 'white');
			$('.form-select').addClass('bg-light text-black');
			$('.form-select').removeClass('bg-dark text-white');
			$('.list-group-item').addClass('bg-light');
			$('.list-group-item').css('color', 'rgb(33, 37, 41)');
			$('.content-wrapper').addClass('bg-light');
			$('.card').addClass('bg-light');
			$('.card-body').css('border', '1px solid rgba(0,0,0,.175)');
			$('.bg-light').addClass('bg-light');
			$('.bg-light').removeClass('bg-dark');
			$('.bg-white').addClass('bg-light');
			$('.bg-white').removeClass('bg-black');
			$('.bd-footer').addClass('bg-light');
			$('table').addClass('table-light');
			$('table').removeClass('table-dark');
			$('#editor.ace_editor').removeClass('ace-chaos ace_dark');
			$('#editor.ace_editor').addClass('ace-tm');
		}
	};
	initDarkTheme();
	window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", initDarkTheme);
	window.matchMedia('(prefers-color-scheme: light)').addEventListener("change", initLightTheme);

});

window.onerror = function (msg, url, line, col, error) {
	// Note that col & error are new to the HTML 5 spec and may not be 
	// supported in every browser.  It worked for me in Chrome.
	//let extra = !col ? '' : ('\ncolumn: ' + col);
	//extra += !error ? '' : ('\nerror: ' + error);

	// You can view the information in an alert to see things working like this:
	//alert("Error: " + msg + "\nurl: " + url + "\nline: " + line + extra);
	console.error(msg, url, line, col, error);

	let suppressErrorAlert = true;
	// If you return true, then error alerts (like in older versions of 
	// Internet Explorer) will be suppressed.
	return suppressErrorAlert;
};
