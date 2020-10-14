/*eslint-disable no-console*/
/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "clientValidate|registerServiceWorker" }]*/
/*global forge*/
"use strict";

var logLevel = {
	Trace: 0,
	Debug: 1,
	Information: 2,
	Warning: 3,
	Error: 4
};

var g_AppRootPath = location.pathname.match(/\/([^/]+)\//)[0],
	g_LogPath = g_AppRootPath + "Home/ClientsideLog",
	g_IsDevelopment = window.location.host.match(/:\d+/) !== null,
	g_Version = 'PROJECT_VERSION';

function immediateActions() {
	//change header background depending on developmen/production enviromewnt
	Array.prototype.slice.call(document.querySelectorAll("nav.navbar")).forEach(function (el) {
		el.classList.remove("bg-dark");
		el.classList.add(g_IsDevelopment ? 'bg-dark-development' : 'bg-dark-production');
	});

	//append version to footer
	if (g_Version && g_Version !== '')
		document.getElementById('spVersion').textContent = ", Version: " + g_Version;
}

immediateActions();

function ajaxLog(level, message, url, line, col, error) {
	$.post(g_LogPath, {
		"level": level, "message": message, "url": url, "line": line, "col": col, "error": error
	});
}

function handleLogoutForm() {
	//if we're not seeing logoutForm form - disable secure/authorized links
	if (document.getElementById("logoutForm") === null) {
		["aInkList", "aInkGame", "aInkGameHigh"].forEach(function (link2disable) {
			let el = document.getElementById(link2disable);
			el.removeAttribute("href");
			el.setAttribute("tabindex", "-1");
			el.setAttribute("aria-disabled", "true");
			el.classList.add("disabled");
		});
	}
}

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
 * Registers service worker globally
 * @param {string} rootPath is a path of all pages after FQDN name (ex. https://foo-bar.com/rootPath) or '/' if no root path
 */
function registerServiceWorker(rootPath) {
	if ('serviceWorker' in navigator &&
		(navigator.serviceWorker.controller === null || navigator.serviceWorker.controller.state !== "activated")) {
		const swUrl = rootPath + 'sw.min.js?domain=' + encodeURIComponent(rootPath);

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
 * Global document ready function
 */
$(function () {
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

	registerServiceWorker(g_AppRootPath, g_Version);

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

///////////////////WebCamGallery functions start/////////////////
/**
 * WebCamGallery onload event
 * @param {boolean} enableAnnualMovieGenerator
 * @param {any} liveImageExpireTimeInSeconds
 */
function WebCamGalleryOnLoad(enableAnnualMovieGenerator, liveImageExpireTimeInSeconds) {
	let last_refresh = new Date();

	/**
	 * on live img refresh click
	 */
	function RefreshLiveImage() {
		const live = document.querySelector("#live");
		if (live != null) {
			const data_last_modified = live.getAttribute('data-last-modified');
			if (data_last_modified != "refreshing") {
				const now = new Date();
				const secs_between = (now - last_refresh) * 0.001;
				let msg = String(secs_between) + ' secs elapsed since last live-image load';
				if (secs_between > liveImageExpireTimeInSeconds) {
					msg += ', reloading!';
					LoadImageAsBinaryArray(live);
				}
				console.log(msg);
			}
			else
				console.log('still reloading!');
		}
	}

	/**
	 * check_webp_feature (taken from https://developers.google.com/speed/webp/faq#how_can_i_detect_browser_support_for_webp)
	 * @param {any} feature can be one of 'lossy', 'lossless', 'alpha' or 'animation'.
	 * @param {any} callback 'callback(result)' will be passed back the detection result (in an asynchronous way!)
	 */
	function check_webp_feature(feature, callback) {
		const kTestImages = {
			lossy: "UklGRiIAAABXRUJQVlA4IBYAAAAwAQCdASoBAAEADsD+JaQAA3AAAAAA",
			lossless: "UklGRhoAAABXRUJQVlA4TA0AAAAvAAAAEAcQERGIiP4HAA==",
			alpha: "UklGRkoAAABXRUJQVlA4WAoAAAAQAAAAAAAAAAAAQUxQSAwAAAARBxAR/Q9ERP8DAABWUDggGAAAABQBAJ0BKgEAAQAAAP4AAA3AAP7mtQAAAA==",
			animation: "UklGRlIAAABXRUJQVlA4WAoAAAASAAAAAAAAAAAAQU5JTQYAAAD/////AABBTk1GJgAAAAAAAAAAAAAAAAAAAGQAAABWUDhMDQAAAC8AAAAQBxAREYiI/gcA"
		};
		const img = new Image();
		img.onload = function () {
			const result = (img.width > 0) && (img.height > 0);
			callback(result);
		};
		img.onerror = function () {
			callback(false);
		};
		img.src = "data:image/webp;base64," + kTestImages[feature];
	}

	function LoadFirstGallerImages() {
		$('img.active:not([src])').each(function (index, value) {
			const img = value;
			const alt = img.alt;
			if (alt && alt !== 'no img') {
				img.src = g_AppRootPath + 'WebCamImages/' + alt;

				const source = img.parentNode.getElementsByTagName('source')[0];
				source.type = "image/webp";
				source.srcset = g_AppRootPath + 'WebCamImages/' + alt.replace(".jpg", ".webp");
			}
		});
	}

	function LoadVideoJS() {
		const player = videojs('my-player');
	}

	function LoadYouTubeIFrame() {
		Array.prototype.slice.call(document.querySelectorAll("#youtube-tab iframe:not([src])")).forEach(function (ytb) {
			ytb.style.display = 'block';
			ytb.src = ytb.dataset.src;
		});
	}

	function ReplImg(event) {
		const el = event.currentTarget || event;
		if (el.classList.contains('active')) return;

		const thumb_url = el.parentNode.parentNode.href.replace("thumbnail", "out").replace(".jpg", "").replace(".webp", "");
		el.src = thumb_url + ".jpg";
		el.alt = "thumbnail-" + thumb_url.split(/thumbnail-(\d+)/)[1];

		const source = el.parentNode.getElementsByTagName('source')[0];
		if (source.srcset === "" || source.srcset === "images/no_img.svg") {
			source.type = "image/webp";
			source.srcset = thumb_url + ".webp";
		}

		el.classList.remove("inactive");
		el.classList.add('active');
	}

	function ReplAllImg() {
		$("img.inactive").each(function (index, value) {
			ReplImg(value);
		});
	}

	function LoadBlueImpGallery(event) {
		event = event || window.event;
		event.preventDefault();

		check_webp_feature('lossy', function (isWebPSupported) {
			const target = event.target || event.srcElement,
				links = target.parentNode.parentNode.parentNode,
				link = target.src ? target.parentNode.parentNode : target,
				options = {
					//index: link,
					event: event,
					onopen: function () {
						// Callback function executed when the Gallery is initialized
						ReplAllImg();
					},
				};

			const urls = Array.prototype.slice.call(links.getElementsByTagName('a')).map(function (a) {
				let href, type;
				if (isWebPSupported) {//webp supported
					href = a.href.replace(".jpg", ".webp");
					type = 'image/webp';
				}
				else {
					href = a.href;
					type = 'image/jpeg';
				}
				return {
					title: a.title,
					href: href.replace("thumbnail", "out"),
					type: type,
					thumbnail: href
				}
			});
			//calculate clicked image index in url list
			const tmp = isWebPSupported ? link.href.replace(".jpg", ".webp") : link.href;
			options.index = -1;
			urls.some(function (value, i) {
				if (value.thumbnail == tmp) {
					options.index = i;
					return true;
				}
			});

			blueimp.Gallery(urls, options);
		});
	}

	function LoadImageAsBinaryArray(img) {
		img.setAttribute('data-last-modified', 'refreshing');

		// Simulate a call to Dropbox or other service that can
		// return an image as an ArrayBuffer.
		const xhr = new XMLHttpRequest();
		// Use JSFiddle logo as a sample image to avoid complicating
		// this example with cross-domain issues.
		xhr.open("GET", g_AppRootPath + "WebCamImages/?handler=live", true);
		xhr.setRequestHeader('Cache-Control', 'no-cache');
		// Ask for the result as an ArrayBuffer.
		xhr.responseType = "arraybuffer";
		xhr.onload = function (e) {
			// Obtain a blob: URL for the image data.
			const arrayBufferView = new Uint8Array(this.response);
			const blob = new Blob([arrayBufferView], { type: "image/jpeg" });

			const hdr_last_modified = this.getResponseHeader('Last-Modified');

			const urlCreator = window.URL || window.webkitURL;
			const imageUrl = urlCreator.createObjectURL(blob);
			img.onload = function () {
				urlCreator.revokeObjectURL(this.src);
			}
			img.src = imageUrl;
			img.setAttribute('data-last-modified', hdr_last_modified);

			last_refresh = new Date();
		};
		xhr.send();
	}

	function GenerateAnnualMovie(event) {
		const hedrs = { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
		const serialized_bag = JSON.stringify({ Result: "query", Product: ["qqq", "xxxx", "yyyy", "zzzzzz"] });
		$('#tbAnnualMovieGenerator').html(
			'<thead><tr>' +
			'<th scope="col">#</th>' +
			'<th scope="col">Name</th>' +
			'<th scope="col">Hash</th>' +
			'<th scope="col">Date</th>' +
			'</tr></thead><caption>Loading...</caption><tbody></tbody>'
		);

		$.ajax({
			method: 'POST',
			url: 'AnnualTimelapse/?handler=SecretAction',
			contentType: "application/json",
			dataType: 'json',
			data: serialized_bag,
			headers: hedrs
		}).done(function (response, textStatus, jqXHR) {
			console.log(response);
			if (response.Result == "error") {
				alert("error");
				return;
			}
			//const stringified = JSON.stringify(response.product, null, 2);
			//display.text(stringified);

			$('#tbAnnualMovieGenerator caption').remove();
			$(response.product).each(function (index, item) {
				$('#tbAnnualMovieGenerator tbody').append(
					'<tr>' +
					'<td>' + item[0] + '</td>' +
					'<td>' + item[1] + '</td>' +
					'<td>' + item[2] + '</td>' +
					'<td>' + item[3] + '</td>' +
					'</tr>'
				);
			});
		}).fail(function (jqXHR, textStatus, errorThrown) {
			alert("error: " + textStatus + " " + errorThrown);
			$('#tbAnnualMovieGenerator').html('');
		}).always(function () {
			event.target.disabled = '';
		});
	}

	//live image anchor tag refresh
	$('#aLive').click(RefreshLiveImage);

	//bluimp-gallery handling
	$('#links').click(LoadBlueImpGallery);

	$('#btnReplAllImg').click(ReplAllImg);

	$("img.inactive").each(function (index, value) {
		if (index < 7) {
			ReplImg(value);
		}
		else {
			const empty = "images/no_img.svg";

			if (!value.onmouseover)
				value.onmouseover = ReplImg;
			const empty_img = g_AppRootPath + empty;
			value.src = empty_img;

			const source = value.parentNode.getElementsByTagName('source')[0];
			source.type = "image/svg+xml";
			source.srcset = empty;
		}
	});

	const liveImgAddr = g_AppRootPath + 'WebCamImages/?handler=live';

	window.onpopstate = function (event) {
		//console.log("location: " + document.location + ", state: " + JSON.stringify(event.state));
		const name = (event.state ? event.state.foo : location.hash) || "#live-tab";
		const tab = $("#myTab a[href='" + name + "']");
		if (tab !== undefined)
			tab.tab('show');

		if (name === '#gallery-tab') {
			$('#btnReplAllImg').prop('disabled', false);
			LoadFirstGallerImages();
		}
		else if (name === '#live-tab') {
			$('#live').attr('src', liveImgAddr);
		}
		else if (name === '#youtube-tab') {
			LoadYouTubeIFrame();
		}
		else if (name === '#video-tab') {
			LoadVideoJS();
		}
		else
			$('#btnReplAllImg').prop('disabled', true);
	};
	if (location.hash !== undefined && location.hash.length > 0) {
		const name = location.hash;
		const tab = $("#myTab a[href='" + name + "']");
		if (tab !== undefined)
			tab.tab('show');

		if (name === '#gallery-tab') {
			$('#btnReplAllImg').prop('disabled', false);
			LoadFirstGallerImages();
		}
		else if (name === '#video-tab') {
			LoadVideoJS();
		}
		else if (name === '#youtube-tab') {
			LoadYouTubeIFrame();
		}
		else
			$('#btnReplAllImg').prop('disabled', true);
	}
	else
		$('#btnReplAllImg').prop('disabled', true);

	if ($("#myTab a.active").length <= 0)
		$("#myTab a").first().tab('show');
	if ($("#myTab a[href='#gallery-tab']").hasClass('active')) {
		$('#btnReplAllImg').prop('disabled', false);
		LoadFirstGallerImages();
	}
	else if ($("#myTab a[href='#video-tab']").hasClass('active')) {
		LoadVideoJS();
	}
	else if ($("#myTab a[href='#youtube-tab']").hasClass('active')) {
		LoadYouTubeIFrame();
	}
	else if ($("#myTab a[href='#live-tab']").hasClass('active')) {
		$('#live').attr('src', liveImgAddr);
	}
	$("#myTab a").on('click', function (e) {
		if (e.target.hash !== undefined) {
			if (e.target.hash.indexOf('gallery-tab') !== -1) {
				$('#btnReplAllImg').prop('disabled', false);
				LoadFirstGallerImages();
			}
			else if (e.target.hash.indexOf('live-tab') !== -1) {
				$('#live').attr('src', liveImgAddr);
				$('#btnReplAllImg').prop('disabled', true);
			}
			else if (e.target.hash.indexOf('video-tab') !== -1) {
				LoadVideoJS();
			}
			else if (e.target.hash.indexOf('youtube-tab') !== -1) {
				LoadYouTubeIFrame();
			}
			else
				$('#btnReplAllImg').prop('disabled', true);

			const addr = e.target.hash;
			let stateObj = {
				foo: addr
			};
			window.history.pushState(stateObj, addr, addr);
		}
	});

	//const enableAnnualMovieGenerator = JSON.parse('@Model.EnableAnnualMovieGenerator'.toLowerCase());
	if (enableAnnualMovieGenerator === true) {
		$('#btnAnnualMovieGenerator').prop('disabled', false).on('click', function (event) {
			if (event.target.attributes['aria-expanded'].value !== 'true') {
				event.target.disabled = 'disabled';
				GenerateAnnualMovie(event);
			}
			$('#divAnnualMovieGenerator').collapse('toggle');
		});
		$('#secretAction').show();
	}
	else {
		$('#secretAction').hide();
	}
}
///////////////////WebCamGallery functions end///////////////////