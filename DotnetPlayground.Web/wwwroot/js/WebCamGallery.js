/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "WebCamGalleryOnLoad" }]*/
/* eslint-disable no-console */
/*global g_AppRootPath, videojs, blueimp, i18next, bootstrap*/
"use strict";

function WebCamGalleryOnLoad(liveImageExpireTimeInSeconds) {
	let last_refresh = new Date();
	const btnReplAllImg = document.getElementById("btnReplAllImg");

	function show(el) {
		if (el) el.style.display = "";
	}

	function hide(el) {
		if (el) el.style.display = "none";
	}

	function getAntiForgeryToken() {
		const token = document.querySelector('input[name="__RequestVerificationToken"]');
		return token ? token.value : "";
	}

	async function parseJsonOrText(response) {
		const contentType = response.headers.get("content-type") || "";
		if (contentType.includes("application/json")) {
			return response.json();
		}
		const text = await response.text();
		try {
			return JSON.parse(text);
		} catch (_error) {
			void _error;
			return text;
		}
	}

	async function postJson(url, antiForgeryToken, payload) {
		const response = await fetch(url, {
			method: "POST",
			headers: {
				"Content-Type": "application/json",
				"RequestVerificationToken": antiForgeryToken
			},
			body: payload,
			credentials: "same-origin"
		});

		if (!response.ok) {
			throw new Error(response.status + " " + response.statusText);
		}
		return parseJsonOrText(response);
	}

	async function RefreshLiveImage() {
		const live = document.getElementById("live");
		if (!live) return;
		const dataLastModified = live.getAttribute("data-last-modified");
		if (dataLastModified === "refreshing") {
			console.log(i18next.t("stillRel"));
			return;
		}

		const now = new Date();
		const secs_between = (now - last_refresh) * 0.001;
		let msg = String(secs_between) + i18next.t("webCam.secsElapsed");
		if (secs_between > liveImageExpireTimeInSeconds) {
			msg += i18next.t("webCam.reloading");
			await LoadImageAsBinaryArray(live);
		}
		console.log(msg);
	}

	function LoadFirstGalleryImages() {
		Array.from(document.querySelectorAll("img.active:not([src])")).forEach(function (img) {
			const alt = img.alt;
			if (!alt || alt === "no img") return;
			const baseURL = g_AppRootPath + "WebCamImages/";
			img.src = baseURL + alt;

			let sourceAvif, sourceWebp;
			const allSources = img.parentNode.getElementsByTagName("source");
			if (allSources.length <= 0) {
				sourceAvif = document.createElement("source");
				img.parentNode.insertBefore(sourceAvif, img);
				sourceWebp = document.createElement("source");
				img.parentNode.insertBefore(sourceWebp, img);
			} else {
				sourceAvif = allSources[0];
				sourceWebp = allSources[1];
			}
			sourceAvif.type = "image/avif";
			sourceAvif.srcset = baseURL + alt.replace(".jpg", ".avif");
			sourceWebp.type = "image/webp";
			sourceWebp.srcset = baseURL + alt.replace(".jpg", ".webp");
		});
	}

	function LoadVideoJS() {
		const myPlayer = document.getElementById("my-player");
		if (!myPlayer || !myPlayer.dataset.poster) return;
		myPlayer.setAttribute("poster", myPlayer.dataset.poster);
		delete myPlayer.dataset.poster;
		videojs("my-player");
	}

	function LoadYouTubeIFrame() {
		Array.from(document.querySelectorAll("#youtube-tab iframe:not([src])")).forEach(function (ytb) {
			ytb.style.display = "block";
			ytb.src = ytb.dataset.src;
		});
	}

	function ReplImg(event) {
		const el = event.currentTarget || event;
		if (el.classList.contains("active")) return;

		const thumbUrl = el.parentNode.parentNode.href.replace(/\.[^.]*$/, "");
		el.src = thumbUrl + ".jpg";
		el.alt = "thumbnail-" + thumbUrl.split(/thumbnail-(\d+)/)[1];

		let sourceAvif, sourceWebp;
		const allSources = el.parentNode.getElementsByTagName("source");
		if (allSources.length <= 0) {
			sourceAvif = document.createElement("source");
			el.parentNode.insertBefore(sourceAvif, el);
			sourceWebp = document.createElement("source");
			el.parentNode.insertBefore(sourceWebp, el);
		} else {
			sourceAvif = allSources[0];
			sourceWebp = allSources[1];
		}

		if (sourceAvif.srcset === "" || sourceAvif.srcset === "images/no_img.svg") {
			sourceAvif.type = "image/avif";
			sourceAvif.srcset = thumbUrl + ".avif";
		}
		if (sourceWebp.srcset === "" || sourceWebp.srcset === "images/no_img.svg") {
			sourceWebp.type = "image/webp";
			sourceWebp.srcset = thumbUrl + ".webp";
		}

		el.classList.remove("inactive");
		el.classList.add("active");
	}

	function ReplAllImg() {
		Array.from(document.querySelectorAll("img.inactive")).forEach(function (img) {
			ReplImg(img);
		});
	}

	function LoadBlueImpGallery(event) {
		event = event || window.event;
		event.preventDefault();
		const target = event.target || event.srcElement;
		const links = target.parentNode.parentNode.parentNode;
		const link = target.src ? target.parentNode.parentNode : target;
		const options = {
			event: event,
			onopen: function () {
				ReplAllImg();
			}
		};

		const urls = Array.from(links.getElementsByTagName("a")).map(function (a) {
			const href = a.href.replace(".jpg", ".avif");
			return {
				title: a.title,
				href: href.replace("thumbnail", "out"),
				type: "image/avif",
				thumbnail: href
			};
		});

		const selected = link.href.replace(".jpg", ".avif");
		options.index = urls.findIndex(function (value) {
			return value.thumbnail === selected;
		});
		blueimp.Gallery(urls, options);
	}

	async function LoadImageAsBinaryArray(img) {
		img.setAttribute("data-last-modified", "refreshing");
		try {
			const response = await fetch(g_AppRootPath + "WebCamImages/?handler=live", {
				method: "GET",
				headers: { "Cache-Control": "no-cache" }
			});
			if (!response.ok) throw new Error(i18next.t("webCam.resNotOk"));
			const hdrLastModified = response.headers.get("Last-Modified");
			const blob = await response.blob();
			const imageUrl = URL.createObjectURL(blob);
			img.onload = function () {
				URL.revokeObjectURL(this.src);
			};
			img.src = imageUrl;
			img.setAttribute("data-last-modified", hdrLastModified);
			last_refresh = new Date();
		} catch (err) {
			img.setAttribute("data-last-modified", new Date().toUTCString());
			last_refresh = new Date();
			alert(err.toString());
		}
	}

	function GenerateAnnualMovie(event) {
		const table = document.getElementById("tbAnnualMovieGenerator");
		const antiForgeryToken = getAntiForgeryToken();
		const serializedBag = JSON.stringify({ Result: "query", Product: ["qqq", "xxxx", "yyyy", "zzzzzz"] });
		table.innerHTML =
			'<thead><tr>' +
			'<th scope="col">#</th>' +
			"<th scope=\"col\" data-i18n='webCam.annColName'>Name</th>" +
			"<th scope=\"col\" data-i18n='webCam.annColHash'>Hash</th>" +
			"<th scope=\"col\" data-i18n='webCam.annColDate'>Date</th>" +
			"</tr></thead><caption data-i18n='webCam.annLoading'>Loading...</caption><tbody></tbody>";
		if (window.localize) window.localize("#tbAnnualMovieGenerator");

		postJson("AnnualTimelapse/?handler=SecretAction", antiForgeryToken, serializedBag).then(function (response) {
			if (response.result === "Error0") {
				alert(i18next.t("webCam.error"));
				return;
			}

			const caption = table.querySelector("caption");
			if (caption) caption.remove();
			const tbody = table.querySelector("tbody");
			(response.product || []).forEach(function (item) {
				const tr = document.createElement("tr");
				tr.innerHTML = `<td>${item[0]}</td><td>${item[1]}</td><td>${item[2]}</td><td>${item[3]}</td>`;
				tbody.appendChild(tr);
			});
		}).catch(function (error) {
			alert(i18next.t("webCam.errorFollowing") + error.message);
			table.innerHTML = "";
		}).finally(function () {
			event.target.disabled = false;
		});
	}

	function showTabByHash(name) {
		const tab = document.querySelector(`#myTab a[href='${name}']`);
		if (tab) {
			bootstrap.Tab.getOrCreateInstance(tab).show();
		}
	}

	function handleTabState(name, liveImgAddr) {
		if (name === "#gallery-tab") {
			show(btnReplAllImg);
			LoadFirstGalleryImages();
		} else if (name === "#live-tab") {
			const live = document.getElementById("live");
			if (live) live.src = liveImgAddr;
			hide(btnReplAllImg);
		} else if (name === "#youtube-tab") {
			LoadYouTubeIFrame();
			hide(btnReplAllImg);
		} else if (name === "#video-tab") {
			LoadVideoJS();
			hide(btnReplAllImg);
		} else {
			hide(btnReplAllImg);
		}
	}

	document.getElementById("aLive")?.addEventListener("click", RefreshLiveImage);
	Array.from(document.querySelectorAll("#links a")).forEach(function (a) {
		a.addEventListener("click", LoadBlueImpGallery);
	});
	btnReplAllImg?.addEventListener("click", ReplAllImg);

	Array.from(document.querySelectorAll("img.inactive")).forEach(function (value, index) {
		if (index < 7) {
			ReplImg(value);
		} else {
			const noImg = "images/no_img.svg";
			if (!value.onmouseover) value.onmouseover = ReplImg;
			value.src = g_AppRootPath + noImg;

			let source = document.createElement("source");
			source.type = "image/svg+xml";
			source.srcset = noImg;
			value.parentNode.insertBefore(source, value);
			source = document.createElement("source");
			source.type = "image/svg+xml";
			source.srcset = noImg;
			value.parentNode.insertBefore(source, value);
		}
	});

	const liveImgAddr = g_AppRootPath + "WebCamImages/?handler=live";

	window.onpopstate = function (event) {
		const name = (event.state ? event.state.foo : location.hash) || "#live-tab";
		showTabByHash(name);
		handleTabState(name, liveImgAddr);
	};

	if (location.hash && location.hash.length > 0) {
		showTabByHash(location.hash);
		handleTabState(location.hash, liveImgAddr);
	} else {
		hide(btnReplAllImg);
	}

	const activeTab = document.querySelector("#myTab a.active") || document.querySelector("#myTab a");
	if (activeTab) {
		bootstrap.Tab.getOrCreateInstance(activeTab).show();
		handleTabState(activeTab.getAttribute("href"), liveImgAddr);
	}

	Array.from(document.querySelectorAll("#myTab a")).forEach(function (a) {
		a.addEventListener("click", function (e) {
			const hash = e.target.hash;
			if (!hash) return;
			handleTabState(hash, liveImgAddr);
			const stateObj = { foo: hash };
			window.history.pushState(stateObj, hash, hash);
		});
	});

	const secretAction = document.getElementById("secretAction");
	if (secretAction && secretAction.offsetParent !== null) {
		const btn = document.getElementById("btnAnnualMovieGenerator");
		if (btn) btn.disabled = false;
		document.getElementById("divAnnualMovieGenerator")?.addEventListener("show.bs.collapse", function (event) {
			GenerateAnnualMovie(event);
		});
	}
}
