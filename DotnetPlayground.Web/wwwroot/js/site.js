﻿/*eslint-disable no-console*/
/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "clientValidate" }]*/
/*global forge*/
"use strict";
//This code is not babel-transpiled to suport legacy browser; must be compatible with ALL

var g_AppRootPath = location.pathname.match(/\/([^/]+)\//)[0],
	g_LogPath = g_AppRootPath + "Home/ClientsideLog",
	g_IsDevelopment = window.location.host.match(/:\d+/) !== null;
//g_Version = 'PROJECT_VERSION';

//executed immediatelly function
/*(function () {
	//change header background depending on developmen/production enviromewnt
	Array.prototype.slice.call(document.querySelectorAll("nav.navbar")).forEach(function (el) {
		el.classList.remove("bg-dark");
		el.classList.add(g_IsDevelopment ? 'bg-dark-development' : 'bg-dark-production');
	});

	//append version to footer
	if (g_Version && g_Version !== '')
		document.getElementById('spVersion').textContent = ", Version: " + g_Version;
})();*/

function clientValidate(button) {
	let tr = $(button).parent().parent();

	let key = tr.find("td").eq(0).text();
	let orig_md5 = tr.find("td").eq(1).text();
	let orig_sha = tr.find("td").eq(2).text();

	if (orig_md5 === '' || orig_sha === '')
		return;

	let md = forge.md.md5.create();
	md.update(key);
	let md5 = md.digest().toHex();
	md = forge.md.sha256.create();
	md.update(key);
	let sha = md.digest().toHex();

	tr.find("td").eq(1).html("<strong style='color:" + (md5 === orig_md5 ? "green" : "red") + "'>" + orig_md5 + "</strong>");
	tr.find("td").eq(2).html("<strong style='color:" + (sha === orig_sha ? "green" : "red") + "'>" + orig_sha + "</strong>");
}

/**
 * https://stackoverflow.com/a/2641047/4429828
 * @param {string} name of event
 * @param {function} fn is a handler function
 */
$.fn.bindFirst = function (name, fn) {
	// Bind as you normally would. Don't want to miss out on any jQuery magic
	this.on(name, fn);

	// Thanks to a comment by @@Martin, adding support for namespaced events too.
	this.each(function () {
		let handlers = $._data(this, 'events')[name.split('.')[0]];
		//console.log(handlers);
		// take out the handler we just inserted from the end
		let handler = handlers.pop();
		// move it at the beginning
		handlers.splice(0, 0, handler);
	});
};

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

	console.debug = function (message) {
		ajaxLog(logLevel.Debug, message);
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
