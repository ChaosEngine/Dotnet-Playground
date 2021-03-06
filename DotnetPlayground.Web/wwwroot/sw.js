﻿/* eslint-disable no-console */
"use strict";
//Offline mode service worker implementation

const CACHE_NAME = 'cache';

self.addEventListener('install', function (event) {
	const swUrl = new URL(self.location);
	const domain = swUrl.searchParams.get('domain');
	let isDev = swUrl.searchParams.get('isDev');
	isDev = isDev === true || isDev === "true" || isDev === 1 || isDev === "1" ? true : false;
	const suffix = isDev ? '' : '.min';

	let RESOURCES = [
		'',
		'Home/About',
		'Home/Contact',
		'Home/UnintentionalErr/',
		'Blogs',
		'Blogs/Create',
		'WebCamGallery',
		'WebCamImages/?handler=live',
		'VirtualScroll/',
		'VirtualScroll/Load?search=&offset=0&limit=50&ExtraParam=cached',
		'Hashes/',
		'BruteForce/',
		'InkBall/Home',
		'InkBall/Rules',

		'images/favicon.png',
		'images/banner1.svg',
		'images/banner2.svg',
		'images/banner3.svg',
		'images/banner4.svg',
		'images/no_img.svg',
		'img/homescreen.webp',
		'img/homescreen.jpg',
		'https://haos.hopto.org/webcamgallery/poster.jpeg',

		`js/workers/shared${suffix}.js`,
		`js/workers/BruteForceWorker${suffix}.js`,
		`js/BruteForce${suffix}.js`,
		`css/site${suffix}.css`,
		`js/site${suffix}.js`,
		`js/Blogs${suffix}.js`,
		`js/WebCamGallery${suffix}.js`
	];

	if (isDev) {
		RESOURCES = RESOURCES.concat([
			'lib/jquery/jquery.min.js',
			'lib/bootstrap/css/bootstrap.min.css',
			'lib/bootstrap/js/bootstrap.bundle.min.js',
			'lib/blueimp-gallery/css/blueimp-gallery.min.css',
			'lib/blueimp-gallery/js/blueimp-gallery.min.js',
			'lib/video.js/video-js.min.css',
			'lib/video.js/alt/video.core.novtt.min.js',
			'lib/forge/forge.min.js',
			'lib/bootstrap-table/bootstrap-table.min.css',
			'lib/bootstrap-table/bootstrap-table.min.js',
			//'lib/faker/faker.min.js',//questionable, big script ?
			'lib/jquery-validation/jquery.validate.min.js',
			'lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js',
			'lib/ace-builds/ace.js',//questionable, only dev ?
			'lib/ace-builds/mode-csharp.js'//questionable, only dev ?
			//'lib/signalr/browser/signalr.min.js',//questionable, inkball ?
			//'lib/msgpack5/msgpack5.min.js',//questionable, inkball ?
			//'lib/signalr-protocol-msgpack/browser/signalr-protocol-msgpack.min.js'//questionable, inkball ?
		]);
	}
	else {
		//cdn resources
		RESOURCES = RESOURCES.concat([
			'https://cdnjs.cloudflare.com/ajax/libs/blueimp-gallery/3.3.0/css/blueimp-gallery.min.css',
			'https://cdn.jsdelivr.net/npm/video.js@7.11.4/dist/video-js.min.css',
			'https://cdnjs.cloudflare.com/ajax/libs/bootstrap-table/1.18.2/bootstrap-table.min.css',
			'https://cdn.jsdelivr.net/npm/bootstrap@4.6.0/dist/css/bootstrap.min.css',
			'https://cdnjs.cloudflare.com/ajax/libs/blueimp-gallery/3.3.0/js/blueimp-gallery.min.js',
			'https://cdn.jsdelivr.net/npm/video.js@7.11.4/dist/alt/video.core.novtt.min.js',
			'https://cdnjs.cloudflare.com/ajax/libs/bootstrap-table/1.18.2/bootstrap-table.min.js',
			//'https://cdn.jsdelivr.net/npm/faker@5.5.2/dist/faker.min.js',//questionable, big script ?
			'https://cdn.jsdelivr.net/npm/node-forge@0.10.0/dist/forge.min.js',
			'https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js',
			'https://cdn.jsdelivr.net/npm/bootstrap@4.6.0/dist/js/bootstrap.bundle.min.js',
			'https://cdnjs.cloudflare.com/ajax/libs/jquery-validate/1.19.2/jquery.validate.min.js',
			'https://cdn.jsdelivr.net/npm/jquery-validation-unobtrusive@3.2.12/dist/jquery.validate.unobtrusive.min.js'
			//'https://cdnjs.cloudflare.com/ajax/libs/ace/1.4.12/ace.js',//questionable, only dev ?
			//'https://cdnjs.cloudflare.com/ajax/libs/ace/1.4.12/mode-csharp.min.js',//questionable, only dev ?
			//'https://cdn.jsdelivr.net/npm/@microsoft/signalr@5.0.4/dist/browser/signalr.min.js',//questionable, inkball ?
			//'https://cdn.jsdelivr.net/npm/msgpack5@5.3.2/dist/msgpack5.min.js',//questionable, inkball ?
			//'https://cdn.jsdelivr.net/npm/@microsoft/signalr-protocol-msgpack@5.0.4/dist/browser/signalr-protocol-msgpack.min.js'//questionable, inkball ?
		]);
	}


	event.waitUntil(
		caches.open(CACHE_NAME).then(async (cache) => {
			//1st load cross-orign stuff with opaque response (risky but....)
			RESOURCES.filter(res => res.indexOf("http") === 0).map(async (crossOriginUrl) => {
				const crossRequest = new Request(crossOriginUrl, { mode: 'no-cors' });
				const response = await fetch(crossRequest);
				await cache.put(crossRequest, response);
			});
			//then load local domain requests
			await cache.addAll(RESOURCES.filter(res => res.indexOf("http") !== 0).map(localPath => {
				return domain + localPath;
			}));
		})
	);
});

self.addEventListener("activate", function (event) {
	event.waitUntil(
		caches.keys().then(function (allCacheNames) {
			return Promise.all(
				allCacheNames.map(function (cn) {
					if (CACHE_NAME !== cn) {
						console.log('Service Worker deleting cache ' + cn);
						return caches.delete(cn);
					}
				})
			);
		})
	);
});

self.addEventListener('fetch', event => {
	event.respondWith(caches.match(event.request, { ignoreVary: true }).then(cached_response => {
		// caches.match() always resolves
		// but in case of success response will have value
		// if (response)
		// 	return response;

		return fetch(event.request).then(response => {
			// Check if we received a valid response
			if (!response || response.status !== 200 || response.type !== 'basic') {
				return response || fetch(event.request);
			}

			if (event.request.method !== 'GET')
				return response;

			// response may be used only once
			// we need to save clone to put one copy in cache
			// and serve second one
			const responseClone = response.clone();

			caches.open(CACHE_NAME).then(cache => {
				cache.put(event.request, responseClone);
			});
			return response;
		}).catch(() => {
			return cached_response;
		});

	}));
});
