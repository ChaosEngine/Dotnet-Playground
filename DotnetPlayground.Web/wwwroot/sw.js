/* eslint-disable no-console */
"use strict";
//Offline mode service worker implementation

const CACHE_NAME = 'cache';

self.addEventListener('install', function (event) {
	const url = new URL(location);
	const domain = url.searchParams.get('domain');
	const isDev = url.searchParams.get('isDev');
	const suffix = isDev === true || isDev === "true" || isDev === 1 || isDev === "1" ? '' : '.min';

	const RESOURCES = [
		'',
		'Home/About',
		'Home/Contact',
		'Home/UnintentionalErr/',
		'Blogs',
		'Blogs/Create',
		'WebCamGallery',
		'WebCamImages/?handler=live',
		'VirtualScroll/',
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

		'lib/jquery/dist/jquery.min.js',
		'lib/bootstrap/dist/css/bootstrap.min.css',
		'lib/bootstrap/dist/js/bootstrap.bundle.min.js',
		'lib/blueimp-gallery/css/blueimp-gallery.min.css',
		'lib/blueimp-gallery/js/blueimp-gallery.min.js',
		'lib/video.js/dist/video-js.min.css',
		'lib/video.js/dist/video.min.js',
		'lib/forge/dist/forge.min.js',
		//'js/workers/shared.js',
		`js/workers/shared${suffix}.js`,
		//'js/workers/BruteForceWorker.js',
		`js/workers/BruteForceWorker${suffix}.js`,
		//'css/site.css',
		`css/site${suffix}.css`,
		//'js/site.js',
		`js/site${suffix}.js`
	];


	event.waitUntil(
		caches.open(CACHE_NAME).then(function (cache) {
			return cache.addAll(RESOURCES.map(res => domain + res));
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
