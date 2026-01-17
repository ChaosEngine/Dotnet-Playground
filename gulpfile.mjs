"use strict";

import gulp from 'gulp';
// const { series, parallel, src, dest, task } = gulp;

import process from 'node:process';
import fs from 'node:fs/promises';
import path from 'node:path';

import * as dartSass from 'sass';
import gulpSass from 'gulp-sass';
const sass = gulpSass(dartSass);

import replace from 'gulp-replace';
import concat from "gulp-concat";
import cleanCSS from "@aptuitiv/gulp-clean-css";
import terser from "gulp-terser";
//import babel from "gulp-babel",
import rename from "gulp-rename";
import sourcemaps from 'gulp-sourcemaps';
import webpack from 'webpack-stream';
// import workerPlugin from 'worker-plugin';
// import TerserPlugin from 'terser-webpack-plugin';
import jsonMinify from 'gulp-json-minify';

const webroot = "./DotnetPlayground.Web/wwwroot/";
const IBwebroot = "./InkBall/src/InkBall.Module/wwwroot/";

const paths = {
	js: webroot + "js/**/*.js",
	minJs: webroot + "js/**/*.min.js",
	css: webroot + "css/**/*.css",
	scss: webroot + "css/**/*.scss",
	minCss: webroot + "css/**/*.min.css",
	translation: webroot + "locales/**/*.json",
	minTranslation: webroot + "locales/**/*.min.json",
	destCSSDir: webroot + "css/",
	concatJsDest: webroot + "js/site.min.js",
	//<ServiceWorker>
	SWJs: webroot + "sw.js",
	SWJsDest: webroot + "sw.min.js",
	//<ServiceWorker>
	//<WebWorkers>
	BruteForceWorkerJs: webroot + "js/workers/BruteForceWorker.js",
	BruteForceWorkerJsDest: webroot + "js/workers/BruteForceWorker.min.js",
	SharedJs: webroot + "js/workers/shared.js",
	SharedJsDest: webroot + "js/workers/shared.min.js",
	//</WebWorkers>
	//<InkBall>
	inkBallJsRelative: IBwebroot + "js/",
	inkBallCssRelative: IBwebroot + "css/",
	inkBallTranslation: IBwebroot + "locales/**/*.json",
	inkBallMinTranslation: IBwebroot + "locales/**/*.min.json"
	//</InkBall>
};

const minCSS = function (sourcePattern, notPattern, dest) {
	return gulp.src([sourcePattern, "!" + notPattern])
		.pipe(concat(dest))
		.pipe(sourcemaps.init())
		.pipe(cleanCSS())
		.pipe(sourcemaps.mapSources(function (sourcePath, file) {
			// console.log(`OMG-mapSources A '${sourcePath}', '${file.basename}' Z`);
			return file.basename.replace('.min', '');
		}))
		.pipe(sourcemaps.write('./', { includeContent: false }))
		.pipe(gulp.dest("."));
};

const rimraf = async function (globPattern) {
	//const found_files = await glob(globPattern);
	let found_files = [];
	for await (const file of fs.glob(globPattern))
		found_files.push(file);

	await Promise.all(found_files.map(file => {
		// console.log(file);
		return fs.rm(file);
	}));
};

////////////// [Inkball Section] //////////////////
// eslint-disable-next-line no-unused-vars
const inkballEntryPoint = function (min) {
	return gulp.src([
		paths.inkBallJsRelative + 'inkball.js'
		//paths.inkBallJsRelative + 'shared.js',
		//paths.inkBallJsRelative + 'AISource.js'
	]).pipe(webpack({
		entry: {
			'inkball': [
				//'@babel/polyfill',
				paths.inkBallJsRelative + 'inkball.js'
			]
		},
		output: {
			filename: '[name].Bundle.js',
			chunkFilename: '[name].Bundle.js',
			publicPath: '../js/'
		},
		//plugins: [
		//	new workerPlugin({
		//		// use "self" as the global object when receiving hot updates.
		//		globalObject: 'self' // <-- this is the default value
		//	})
		//],
		module: {
			rules: [{
				use: {
					loader: 'babel-loader',
					options: {
						presets: [
							["@babel/preset-env", { "useBuiltIns": "entry", "corejs": 3 }]
						]
					}
				}
			}]
		},
		optimization: {
			minimize: min
			//, minimizer: [
			// 	new TerserPlugin({
			// 		extractComments: false
			// 	})
			// ]
		},
		performance: {
			hints: process.env.NODE_ENV === 'production' ? "warning" : false
		},
		mode: "production",
		stats: "errors-warnings"
	}))
		.pipe(rename({ suffix: min ? '.min' : '' }))
		.pipe(gulp.dest(paths.inkBallJsRelative));
};

const inkballAIWorker = function (doPollyfill) {
	return gulp.src(paths.inkBallJsRelative + "AIWorker.js")
		.pipe(webpack({
			entry: {
				'AIWorker': doPollyfill === true ? [
					'@babel/polyfill',
					paths.inkBallJsRelative + 'AIWorker.js'
				] : [
					paths.inkBallJsRelative + 'AIWorker.js'
				]
			},
			// target: "webworker",
			output: {
				filename: doPollyfill === true ? '[name].PolyfillBundle.js' : '[name].Bundle.js'
			},
			// plugins: [
			// 	new workerPlugin({
			// 		// use "self" as the global object when receiving hot updates.
			// 		globalObject: 'self' // <-- this is the default value
			// 	})
			// ],
			module: doPollyfill === true ? {
				rules: [{
					use: {
						loader: 'babel-loader',
						options: {
							presets: [
								["@babel/preset-env", { "useBuiltIns": "entry", "corejs": 3 }]
							]
							//, plugins: [
							//	"@babel/plugin-transform-runtime"
							//]
						}
					}
				}]
			} : {},
			optimization: {
				minimize: true
				//, minimizer: [
				//	new TerserPlugin({
				//		extractComments: false
				//	})
				//]
			},
			performance: {
				hints: process.env.NODE_ENV === 'production' ? "warning" : false
			},
			mode: "production",
			stats: "errors-warnings",
			devtool: 'source-map'
		}))
		.pipe(gulp.dest(paths.inkBallJsRelative));
};

const webpackRun = gulp.parallel(function inkballWebWorkerEntryPoint(cb) {
	// inkballAIWorker(true);
	inkballAIWorker(false);
	return cb();
}/*, function inkballMainEntryPoint(cb) {
	inkballEntryPoint(false);
	inkballEntryPoint(true);
	return cb();
}*/);

const fileMinifyJSFunction = function (src, dest, toplevel = false) {
	return gulp.src([src, "!" + dest], { base: "." })
		.pipe(concat(dest))
		.pipe(sourcemaps.init())
		.pipe(terser({
			toplevel: toplevel
		}))
		.pipe(sourcemaps.mapSources(function (sourcePath, file) {
			// console.log(`OMG-mapSources A '${sourcePath}', '${file.basename}' Z`);
			return file.basename.replace('.min', '');
		}))
		.pipe(sourcemaps.write('./', { includeContent: false }))
		.pipe(gulp.dest("."));
};

const fileMinifySCSSFunction = function (src, dest) {
	return gulp.src([src, "!" + dest], { base: "." })
		.pipe(concat(dest))
		.pipe(sass().on('error', sass.logError))
		.pipe(gulp.dest("."));
};

const minInkballJs = gulp.parallel(
	function inkballJs() {
		return fileMinifyJSFunction(paths.inkBallJsRelative + "inkball.js",
			paths.inkBallJsRelative + "inkball.min.js", true);
	},
	function inkballSharedJs() {
		return fileMinifyJSFunction(paths.inkBallJsRelative + "shared.js",
			paths.inkBallJsRelative + "shared.min.js", true);
	}
);

const minInkballCss = gulp.series(
	function inkBallScssToCSS() {
		return fileMinifySCSSFunction(paths.inkBallCssRelative + "inkball.scss", paths.inkBallCssRelative + "inkball.css");
	},
	function inkBallCssToMinCSS() {
		return minCSS(paths.inkBallCssRelative + "inkball.css", paths.inkBallCssRelative + "inkball.min.css",
			paths.inkBallCssRelative + "inkball.min.css");
	}
);

const minInkballTranslations = function concatJsDest() {
	return gulp.src([paths.inkBallTranslation, "!" + paths.inkBallMinTranslation], { base: "." })
		.pipe(jsonMinify())
		.pipe(rename({ suffix: '.min' }))
		.pipe(gulp.dest("."));
};

const minInkball = gulp.parallel(minInkballJs, minInkballCss, minInkballTranslations);

const cleanInkball = async function (cb) {
	await Promise.all([
		rimraf(paths.inkBallJsRelative + "*.min.js"),
		rimraf(paths.inkBallCssRelative + "*.css"),
		rimraf(paths.inkBallCssRelative + "*.map"),
		rimraf(paths.inkBallJsRelative + "*Bundle.js"),
		rimraf(paths.inkBallJsRelative + "*.map")
	]);

	cb();
};
////////////// [/Inkball Section] //////////////////

const cleanJs = gulp.series(cleanInkball, async function cleanMinJs(cb) {
	await Promise.all([
		rimraf(paths.minJs),
		rimraf(paths.SWJsDest),
		rimraf(paths.minTranslation),
		rimraf(paths.inkBallMinTranslation),
		rimraf(webroot + "js/**/*.map"),
		rimraf(webroot + "*.map")
	]);

	cb();
});

const runCleanCss = async function (cb) {
	await Promise.all([
		rimraf(webroot + "css/*.css*"),
		rimraf(webroot + "css/*.map")
	]);

	cb();
};

const clean = gulp.series(cleanJs, runCleanCss);

const minSWJsJs = function () {
	return fileMinifyJSFunction(paths.SWJs, paths.SWJsDest);
};

const minJs = gulp.series(minSWJsJs,
	function concatJsDest() {
		return gulp.src([paths.js, "!" + paths.minJs], { base: "." })
			//.pipe(concat(paths.concatJsDest))
			.pipe(sourcemaps.init())
			.pipe(terser())
			.pipe(rename({ suffix: '.min' }))
			.pipe(sourcemaps.mapSources(function (sourcePath, file) {
				// console.log(`OMG-mapSources A '${sourcePath}', '${file.basename}' Z`);
				return file.basename.replace('.min', '');
			}))
			.pipe(sourcemaps.write('./', { includeContent: false }))
			.pipe(gulp.dest("."));
	}
);

const minTranslations = function concatJsDest() {
	return gulp.src([paths.translation, "!" + paths.minTranslation], { base: "." })
		.pipe(jsonMinify())
		.pipe(rename({ suffix: '.min' }))
		.pipe(gulp.dest("."));
};

const processInputArgs = function () {
	let colorTheme = undefined;//process.env.NODE_ENV === 'production' ? 'darkred' : 'darkslateblue';
	let env = undefined;
	let projectVersion = undefined;
	const argv = process.argv;
	//console.log('pure argv = ' + JSON.stringify(argv));
	const interestingArgs = argv.length > 2 ? argv.splice(2).filter(x => x.startsWith('--')) : undefined;

	if (interestingArgs !== undefined && interestingArgs.length > 0) {
		const params = interestingArgs.map(item => item.split('=', 2)).map(kv => {
			let xxx = {};
			//console.log(kv[0], kv[1]);
			xxx[kv[0].substring(2)] = kv[1];
			return xxx;
		});
		if (params !== null && params.length > 0) {
			for (const par of params) {
				//console.log('par = '+par);
				if (par['version'] !== undefined && par['version'].length > 0)
					projectVersion = par['version'];
				if (par['env'] !== undefined && par['env'].length > 0)
					env = par['env'];
			}
		}
		// eslint-disable-next-line no-console
		console.log('Argv => ' + JSON.stringify(params));
	}

	if (projectVersion !== undefined && projectVersion.length > 0)
		projectVersion = ', Version: ' + projectVersion;
	else
		projectVersion = ', Debug: xx.yy.zz-ssss';

	switch (env) {
		case 'prod':
		case 'production':
			env = 'production';
			colorTheme = 'darkred';
			break;

		case 'dev':
		case 'development':
		default:
			env = 'development';
			colorTheme = 'darkslateblue';
			break;
	}

	return { env, colorTheme, projectVersion };
};

const processSCSS = function (sourcePattern, notPattern) {
	const { colorTheme, projectVersion } = processInputArgs();

	return gulp.src([sourcePattern, "!" + notPattern])
		// .pipe(header('$themeColor: ${color};\n$projectVersion: ${version};\n', { color: colorTheme, version: `'${projectVersion}'` }))
		.pipe(replace('$themeColor', colorTheme))
		.pipe(replace('$projectVersion', `'${projectVersion}'`))
		.pipe(sass().on('error', sass.logError))
		.pipe(gulp.dest(notPattern));
};

const minScss = gulp.series(
	function scssToCss() {
		return processSCSS(paths.scss, paths.destCSSDir);
	},
	function runTaskMinSiteCSS() {
		return minCSS(webroot + "css/site.css", webroot + "css/site.min.css", webroot + "css/site.min.css");
	},
	function runTaskMinIconsCSS() {
		return minCSS(webroot + "css/icons.css", webroot + "css/icons.min.css", webroot + "css/icons.min.css");
	}
);

const min = gulp.parallel(minJs, minInkball, minScss, minTranslations);

const cssRun = gulp.parallel(minInkballCss, minScss);

///
/// postinstall entry point (npm i)
///
const postinstall = async (cb) => {
	const copy_promises = [];
	const file_copy = (src, dst) => copy_promises.push(fs.cp(src, dst));
	const dir_copy = (src, dst, filter = undefined) => copy_promises.push(fs.cp(src, dst, {
		recursive: true, // needed to copy directories
		filter           // your filter function
	}));
	const nm = 'node_modules', dst = `${webroot}lib/`;

	dir_copy(`${nm}/bootstrap/dist/css`, `${dst}bootstrap/css`, async (src) => {
		if ((await fs.lstat(src)).isDirectory() || src.includes(`bootstrap.min.css`)) {
			return true;
		} else {
			return false;
		}
	});
	dir_copy(`${nm}/bootstrap/dist/js`, `${dst}bootstrap/js`, async (src) => {
		if ((await fs.lstat(src)).isDirectory() || src.includes(`bootstrap.bundle.min.js`)) {
			return true;
		} else {
			return false;
		}
	});

	file_copy(`${nm}/bootstrap-table/dist/bootstrap-table.min.css`, `${dst}bootstrap-table/bootstrap-table.min.css`);
	file_copy(`${nm}/bootstrap-table/dist/bootstrap-table.min.js`, `${dst}bootstrap-table/bootstrap-table.min.js`);

	dir_copy(`${nm}/node-forge/dist`, `${dst}node-forge`, async (src) => {
		if ((await fs.lstat(src)).isDirectory() || src.includes(`forge.min.js`)) {
			return true;
		} else {
			return false;
		}
	});
	file_copy(`${nm}/jquery/dist/jquery.min.js`, `${dst}jquery/jquery.min.js`);
	// file_copy(`${nm}/jquery/dist/jquery.min.map`, `${dst}jquery/jquery.min.map`);

	file_copy(`${nm}/jquery-validation/dist/jquery.validate.min.js`, `${dst}jquery-validation/jquery.validate.min.js`);
	file_copy(`${nm}/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js`, `${dst}jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js`);
	dir_copy(`${nm}/blueimp-gallery/img`, `${dst}blueimp-gallery/img`);
	dir_copy(`${nm}/blueimp-gallery/css`, `${dst}blueimp-gallery/css`, async (src) => {
		if ((await fs.lstat(src)).isDirectory() || src.includes(`blueimp-gallery.min.css`)) {
			return true;
		} else {
			return false;
		}
	});
	dir_copy(`${nm}/blueimp-gallery/js`, `${dst}blueimp-gallery/js`, async (src) => {
		if ((await fs.lstat(src)).isDirectory() || src.includes(`${path.sep}blueimp-gallery.min.js`)) {
			// console.log(`T:` + src);
			return true;
		} else {
			// console.log(`F:` + src);
			return false;
		}
	});
	file_copy(`${nm}/video.js/dist/video-js.min.css`, `${dst}video.js/video-js.min.css`);
	file_copy(`${nm}/video.js/dist/alt/video.core.novtt.min.js`, `${dst}video.js/alt/video.core.novtt.min.js`);
	file_copy(`${nm}/qrcodejs/qrcode.min.js`, `${dst}qrcodejs/qrcode.min.js`);
	dir_copy(`${nm}/@microsoft/signalr/dist/browser`, `${dst}signalr/browser`, async (src) => {
		if ((await fs.lstat(src)).isDirectory() || src.includes(`signalr.min.js`)) {
			// console.log(`T:` + src);
			return true;
		} else {
			// console.log(`F:` + src);
			return false;
		}
	});
	dir_copy(`${nm}/@microsoft/signalr-protocol-msgpack/dist/browser`, `${dst}signalr-protocol-msgpack/browser`, async (src) => {
		if ((await fs.lstat(src)).isDirectory() || src.includes(`signalr-protocol-msgpack.min.js`)) {
			// console.log(`T:` + src);
			return true;
		} else {
			// console.log(`F:` + src);
			return false;
		}
	});
	file_copy(`${nm}/msgpack5/dist/msgpack5.min.js`, `${dst}msgpack5/msgpack5.min.js`);
	file_copy(`${nm}/ace-builds/src-min-noconflict/ace.js`, `${dst}ace-builds/ace.js`);
	file_copy(`${nm}/ace-builds/src-min-noconflict/mode-csharp.js`, `${dst}ace-builds/mode-csharp.js`);
	file_copy(`${nm}/ace-builds/src-min-noconflict/theme-chaos.js`, `${dst}ace-builds/theme-chaos.js`);
	file_copy(`${nm}/ace-builds/src-min-noconflict/ext-searchbox.js`, `${dst}ace-builds/ext-searchbox.js`);
	file_copy(`${nm}/ace-builds/src-min-noconflict/ext-settings_menu.js`, `${dst}ace-builds/ext-settings_menu.js`);
	dir_copy(`${nm}/chance/dist`, `${dst}chance`);

	file_copy(`${nm}/i18next/i18next.min.js`, `${dst}i18next/i18next.min.js`);
	file_copy(`${nm}/loc-i18next/loc-i18next.min.js`, `${dst}loc-i18next/loc-i18next.min.js`);
	file_copy(`${nm}/i18next-http-backend/i18nextHttpBackend.min.js`, `${dst}i18next-http-backend/i18nextHttpBackend.min.js`);
	file_copy(`${nm}/i18next-browser-languagedetector/i18nextBrowserLanguageDetector.min.js`, `${dst}i18next-browser-languagedetector/i18nextBrowserLanguageDetector.min.js`);
	file_copy(`${nm}/i18next-localstorage-backend/i18nextLocalStorageBackend.min.js`, `${dst}i18next-localstorage-backend/i18nextLocalStorageBackend.min.js`);
	file_copy(`${nm}/i18next-chained-backend/i18nextChainedBackend.min.js`, `${dst}i18next-chained-backend/i18nextChainedBackend.min.js`);

	file_copy(`${nm}/html2canvas/dist/html2canvas.min.js`, `${dst}html2canvas/html2canvas.min.js`);

	await Promise.all(copy_promises);

	return cb();
};

///
/// Main entry point
///
const main = gulp.series(
	clean,
	webpackRun,
	min
);

///
/// Exports
///
export {
	main as default,
	clean,
	webpackRun as webpack,
	min,
	cssRun as css,
	postinstall
};