/*eslint-disable no-console*/
/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "clientValidate" }]*/
/*global md5, bootstrap, i18next, i18nextBrowserLanguageDetector, i18nextHttpBackend, i18nextChainedBackend, i18nextLocalStorageBackend, locI18next*/
"use strict";

var g_AppRootPath = location.pathname.match(/\/([^/]+)\//)[0], g_isDevelopment = location.host.match(/:\d+/) !== null,
	g_gitBranch = "GIT_BRANCH", g_gitHash = "GIT_HASH", localize = null;

function qs(selector, root) {
	return (root || document).querySelector(selector);
}

function qsa(selector, root) {
	return Array.from((root || document).querySelectorAll(selector));
}

/**
 * SHA-256 hashing using Web Crypto API
 * @param {string} message string to be hashed using SHA-256
 * @returns {Promise<string>} hashed message in hex format
 */
async function getSHA256(message) {
	const msgUint8 = new TextEncoder().encode(message);
	const hashBuffer = await window.crypto.subtle.digest("SHA-256", msgUint8);
	const hashHex = new Uint8Array(hashBuffer).toHex();
	return hashHex;
}

/**
 * Client side hash validation of clicked single hash row
 * @param {HTMLButtonElement} button triggering action
 */
async function clientValidate(button) {
	const tr = button.closest("tr");
	if (!tr) return;
	const cells = tr.querySelectorAll("td");
	if (cells.length < 3) return;

	const key = cells[0].textContent || "";
	const orig_md5 = cells[1].textContent || "";
	const orig_sha = cells[2].textContent || "";

	if (orig_md5 === "" || orig_sha === "") return;

	const md5sum = md5(key);
	const sha = await getSHA256(key);

	cells[1].style.color = md5sum === orig_md5 ? "green" : "red";
	cells[1].style.fontWeight = "bold";
	cells[2].style.color = sha === orig_sha ? "green" : "red";
	cells[2].style.fontWeight = "bold";
}

/**
 * Client side hash validation of all hash rows
 */
async function clientValidateAll() {
	for (const item of qsa("button[value='Validate']")) {
		await clientValidate(item);
	}
}

/**
 * Custom alert bootstrap modal
 * @param {string} msg content shown
 * @param {string} title of the dialog
 * @param {(Element) => void} onCloseCallback callback executed on close
 */
function myAlert(msg = "Content", title = "Modal title", onCloseCallback = undefined) {
	const myModalEl = document.getElementById("divModal");
	const myModal = bootstrap.Modal.getOrCreateInstance(myModalEl, { keyboard: true, backdrop: true });

	if (onCloseCallback) {
		myModalEl.addEventListener("hidden.bs.modal", function listener(e) {
			e.target.removeEventListener(e.type, listener);
			return onCloseCallback.call(this, e);
		});
	}

	myModalEl.querySelector(".modal-body").textContent = msg;
	document.getElementById("divModalLabel").textContent = title;
	myModal.show();
}

window.addEventListener("DOMContentLoaded", function () {
	window.registerLocalizationOnReady = [];

	function handleLocalization(isDev) {
		function renderLocalize() {
			localize("head,body");
		}

		function loadPathFunc([lng], [namespace]) {
			const loadFromCDN = localStorage.getItem("loadFromCDN") === "true" && !isDev;
			switch (namespace) {
				case "ib":
					return loadFromCDN
						? `https://cdn.jsdelivr.net/gh/ChaosEngine/InkBall@${g_gitBranch}/src/InkBall.Module/wwwroot/locales/${lng}/${namespace}.min.json`
						: `${g_AppRootPath}locales/${lng}/${namespace}${isDev ? "" : ".min"}.json`;
				default:
					return loadFromCDN
						? `https://cdn.jsdelivr.net/gh/ChaosEngine/Dotnet-Playground@${g_gitBranch}/DotnetPlayground.Web/wwwroot/locales/${lng}/${namespace}.min.json`
						: `${g_AppRootPath}locales/${lng}/${namespace}${isDev ? "" : ".min"}.json`;
			}
		}

		i18next
			.use(i18nextChainedBackend)
			.use(i18nextBrowserLanguageDetector)
			.init({
				fallbackLng: false,
				supportedLngs: ["en", "pl"],
				ns: ["translation", ...(location.pathname.match(/InkBall/) ? ["ib"] : [])],
				defaultNS: "translation",
				parseMissingKeyHandler: (key, defaultValue) => {
					console.warn(`Missing i18next localization key: ${key}`);
					return defaultValue || key;
				},
				backend: {
					backends: [
						...(isDev ? [] : [i18nextLocalStorageBackend]),
						i18nextHttpBackend
					],
					backendOptions: [
						...(isDev ? [] : [{
							prefix: "i18next_res_",
							expirationTime: 7 * 24 * 60 * 60 * 1000,
							defaultVersion: g_gitHash
						}]),
						{
							loadPath: loadPathFunc
						}
					]
				}
			}, function () {
				localize = locI18next.init(i18next, { useOptionsAttr: true, optionsAttr: "data-i18n-options" });

				if (window.registerLocalizationOnReady.length > 0) {
					window.registerLocalizationOnReady.forEach(callback => typeof callback === "function" && callback(localize));
				}
				renderLocalize();
			});

		qsa("#langDropdown button[title]").forEach(function (button) {
			button.addEventListener("click", function () {
				const lang = button.getAttribute("title");
				i18next.changeLanguage(lang, function () {
					renderLocalize();
				});
			});
		});
	}

	handleLocalization(g_isDevelopment);
});

window.addEventListener("DOMContentLoaded", function () {
	function ajaxLog(level, message, url, line, col, error) {
		const logPath = g_AppRootPath + "Home/ClientsideLog";
		const data = new URLSearchParams();

		const rvt = qs('input[name="__RequestVerificationToken"]');
		if (rvt) data.set("__RequestVerificationToken", rvt.value);

		if (level !== undefined && level !== null) data.set("level", level);
		if (message) data.set("message", message);
		if (url) data.set("url", url);
		if (line !== undefined && line !== null) data.set("line", line);
		if (col !== undefined && col !== null) data.set("col", col);
		if (error !== undefined && error !== null) data.set("error", error);
		navigator.sendBeacon(logPath, data);
	}

	function handleLogoutForm() {
		const links2disable = qs("#logoutForm") === null
			? ["aInkList", "aInkGame", "aInkGameHigh"]
			: ["aInkRegister"];

		links2disable.forEach(id => {
			const el = document.getElementById(id);
			if (!el) return;
			el.setAttribute("tabindex", "-1");
			el.setAttribute("aria-disabled", "true");
			el.classList.add("disabled");
		});
	}

	function registerServiceWorker(rootPath, isDev) {
		if ("serviceWorker" in navigator) {
			const version = encodeURIComponent(g_gitBranch + "_" + g_gitHash);
			const swUrl = `${rootPath}sw${(isDev ? "" : ".min")}.js?version=${version}`;

			navigator.serviceWorker.register(swUrl, { scope: rootPath }).then(() => console.log("Service Worker Registered"));
			navigator.serviceWorker.ready.then(() => console.log("Service Worker Ready"));
		}
	}

	function registerMyAlert() {
		const wrapper = document.createElement("div");
		wrapper.className = "modal fade";
		wrapper.id = "divModal";
		wrapper.tabIndex = -1;
		wrapper.setAttribute("aria-labelledby", "divModalLabel");
		wrapper.setAttribute("aria-hidden", "true");
		wrapper.innerHTML = '<div class="modal-dialog"><div class="modal-content"><div class="modal-header"><h5 class="modal-title text-break" id="divModalLabel">Modal title</h5><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div><div class="modal-body text-break">Content</div><div class="modal-footer"><button type="button" class="btn btn-primary" data-bs-dismiss="modal">Close</button></div></div></div>';
		document.body.appendChild(wrapper);
	}

	function registerThemeChangeHandler() {
		const html = document.documentElement;
		const themeSwitcher = document.getElementById("themeSwitcher");

		const initDarkTheme = function () {
			if (window.matchMedia("(prefers-color-scheme: dark)").matches && localStorage.getItem("bs-theme") === null) {
				html.setAttribute("data-bs-theme", "dark");
			}
		};
		const initLightTheme = function () {
			if (window.matchMedia("(prefers-color-scheme: light)").matches && localStorage.getItem("bs-theme") === null) {
				html.setAttribute("data-bs-theme", "light");
			}
		};

		initDarkTheme();
		window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", initDarkTheme);
		window.matchMedia("(prefers-color-scheme: light)").addEventListener("change", initLightTheme);

		themeSwitcher.addEventListener("click", function () {
			const classes = themeSwitcher.className.split(" ");
			let cur_theme = classes.pop();
			switch (cur_theme) {
				case "system": cur_theme = "light"; break;
				case "light": cur_theme = "dark"; break;
				default: cur_theme = "system"; break;
			}
			classes.push(cur_theme);
			themeSwitcher.className = classes.join(" ");
			themeSwitcher.setAttribute("data-i18n", `[title]nav.themeSwitcher.${cur_theme};[aria-label]nav.themeSwitcher.${cur_theme}`);
			if (localize) localize("#themeSwitcher");

			if (cur_theme === "system") {
				html.removeAttribute("data-bs-theme");
				localStorage.removeItem("bs-theme");
			} else {
				html.setAttribute("data-bs-theme", cur_theme);
				localStorage.setItem("bs-theme", cur_theme);
			}
		});

		const cur_theme = localStorage.getItem("bs-theme") || "system";
		if (cur_theme === "system") html.removeAttribute("data-bs-theme");
		else html.setAttribute("data-bs-theme", cur_theme);

		const classes = themeSwitcher.className.split(" ");
		classes.pop();
		classes.push(cur_theme);
		themeSwitcher.className = classes.join(" ");
		themeSwitcher.setAttribute("data-i18n", `[title]nav.themeSwitcher.${cur_theme};[aria-label]nav.themeSwitcher.${cur_theme}`);
	}

	function updateOnlineStatus() {
		const offlineIndicator = document.getElementById("offlineIndicator");
		if (!offlineIndicator) return;
		const state = navigator.onLine ? "common.online" : "common.offline";
		offlineIndicator.setAttribute("data-i18n", state);
		if (localize) localize("#offlineIndicator");
		offlineIndicator.style.display = "";
	}

	const logLevel = {
		Trace: 0,
		Debug: 1,
		Information: 2,
		Warning: 3,
		Critical: 5,
		Error: 4,
		None: 6
	};

	const org_trace = console.trace;
	const org_debug = console.debug;
	const org_info = console.info;
	const org_warn = console.warn;
	const org_error = console.error;

	console.trace = function (message) {
		ajaxLog(logLevel.Trace, message);
		org_trace.call(this, arguments);
	};
	console.debug = function () {
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

	registerServiceWorker(g_AppRootPath, g_isDevelopment);
	handleLogoutForm();
	window.addEventListener("online", updateOnlineStatus);
	window.addEventListener("offline", updateOnlineStatus);
	if (navigator.onLine === false) updateOnlineStatus();
	registerThemeChangeHandler();
	registerMyAlert();
	window.alert = myAlert;
});

window.onerror = function (msg, url, line, col, error) {
	console.error(msg, url, line, col, error);
	return true;
};
