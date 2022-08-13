/*global require, __dirname, process, exports*/
"use strict";

const gulp = require("gulp"),
	fs = require('fs-extra'),
	sass = require('gulp-sass')(require('sass')),
	header = require('gulp-header'),
	rimraf = require("rimraf"),
	concat = require("gulp-concat"),
	cleanCSS = require("gulp-clean-css"),
	terser = require("gulp-terser"),
	//babel = require("gulp-babel"),
	rename = require("gulp-rename"),
	sourcemaps = require('gulp-sourcemaps'),
	path = require('path'),
	webpack = require('webpack-stream'),
	//esmWebpackPlugin = require("@purtuga/esm-webpack-plugin"),
	workerPlugin = require('worker-plugin'),
	TerserPlugin = require('terser-webpack-plugin');

var webroot = "./wwwroot/";

var paths = {
	js: webroot + "js/**/*.js",
	minJs: webroot + "js/**/*.min.js",
	css: webroot + "css/**/*.css",
	scss: webroot + "css/**/*.scss",
	minCss: webroot + "css/**/*.min.css",
	destCSSDir: webroot + "css/",
	concatCssDest: webroot + "css/site.css",
	concatCssDestMin: webroot + "css/site.min.css",
	concatJsDest: webroot + "js/site.min.js",
	//<ServiceWorker>
	SWJs: webroot + "sw.js",
	SWJsDest: webroot + "sw.min.js",
	//<ServiceWorker/>
	//<WebWorkers>
	BruteForceWorkerJs: webroot + "js/workers/BruteForceWorker.js",
	BruteForceWorkerJsDest: webroot + "js/workers/BruteForceWorker.min.js",
	SharedJs: webroot + "js/workers/shared.js",
	SharedJsDest: webroot + "js/workers/shared.min.js",
	//<WebWorkers/>
	inkBallJsRelative: "../InkBall/src/InkBall.Module/IBwwwroot/js/",
	inkBallCssRelative: "../InkBall/src/InkBall.Module/IBwwwroot/css/"
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

////////////// [Inkball Section] //////////////////
// eslint-disable-next-line no-unused-vars
const inkballEntryPoint = function (min) {
	return gulp.src([
		paths.inkBallJsRelative + 'inkball.js'
		//paths.inkBallJsRelative + 'shared.js',
		//paths.inkBallJsRelative + 'AISource.js'
	]).pipe(webpack({
		resolve: {
			modules: ['node_modules', `../../../../../${path.basename(__dirname)}/node_modules`]
		},
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
			minimize: min,
			minimizer: [
				new TerserPlugin({
					extractComments: false
				})
			]
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
			resolve: {
				modules: ['node_modules', `../../../../../${path.basename(__dirname)}/node_modules`]
			},
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
			plugins: [
				//new esmWebpackPlugin(),
				new workerPlugin({
					// use "self" as the global object when receiving hot updates.
					globalObject: 'self' // <-- this is the default value
				})
			],
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
				minimize: true,
				minimizer: [
					new TerserPlugin({
						extractComments: false
					})
				]
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

exports.webpack = gulp.parallel(function inkballWebWorkerEntryPoint(cb) {
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

const minInkball = gulp.parallel(function inkballJsAndCSS() {
	return fileMinifyJSFunction(paths.inkBallJsRelative + "inkball.js",
		paths.inkBallJsRelative + "inkball.min.js", true);
},
	function inkballSharedJs() {
		return fileMinifyJSFunction(paths.inkBallJsRelative + "shared.js",
			paths.inkBallJsRelative + "shared.min.js", true);
	},
	gulp.series(function scssToCSS() {
		return fileMinifySCSSFunction(paths.inkBallCssRelative + "inkball.scss", paths.inkBallCssRelative + "inkball.css");
	},
		function cssToMinCSS() {
			return minCSS(paths.inkBallCssRelative + "inkball.css", paths.inkBallCssRelative + "inkball.min.css",
				paths.inkBallCssRelative + "inkball.min.css");
		})
);

const cleanInkball = function (cb) {
	rimraf(paths.inkBallJsRelative + "*.min.js", cb);
	rimraf(paths.inkBallCssRelative + "*.css", cb);
	rimraf(paths.inkBallJsRelative + "*Bundle.js", cb);
	rimraf(paths.inkBallJsRelative + "*.map", cb);
};
////////////// [/Inkball Section] //////////////////

const cleanJs = gulp.series(cleanInkball, function cleanMinJs(cb) {
	rimraf(paths.minJs, cb);
	rimraf(paths.SWJsDest, cb);
	rimraf(webroot + "js/**/*.map", cb);
	rimraf(webroot + "*.map", cb);
});

const cleanCss = function (cb) {
	rimraf(paths.concatCssDest, cb);
	rimraf(paths.concatCssDestMin, cb);
	rimraf(webroot + "css/*.map", cb);
};

exports.clean = gulp.series(cleanJs, cleanCss);

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
		.pipe(header('$themeColor: ${color};\n$projectVersion: ${version};\n', { color: colorTheme, version: `'${projectVersion}'` }))
		.pipe(sass().on('error', sass.logError))
		.pipe(gulp.dest(notPattern));
};

const minScss = gulp.series(function scssToCss() {
	return processSCSS(paths.scss, paths.destCSSDir);
}, function runTaskMinCSS() {
	return minCSS(paths.css, paths.minCss, paths.concatCssDestMin);
});

exports.min = gulp.parallel(minJs, minInkball, minScss);

///
/// postinstall entry point (npm i)
///
exports.postinstall = async (cb) => {
	const copy_promises = [];
	const file_copy = (src, dst) => copy_promises.push(fs.copy(src, dst));
	const dir_copy = (src, dst, filter = undefined) => copy_promises.push(fs.copy(src, dst, { filter }));
	const nm = 'node_modules', dst = `${webroot}lib/`;

	dir_copy(`${nm}/bootstrap/dist/css`, `${dst}bootstrap/css`, (src) => {
		if (fs.lstatSync(src).isDirectory() || src.includes(`bootstrap.min.css`)) {
			return true;
		} else {
			return false;
		}
	});
	dir_copy(`${nm}/bootstrap/dist/js`, `${dst}bootstrap/js`, (src) => {
		if (fs.lstatSync(src).isDirectory() || src.includes(`bootstrap.bundle.min.js`)) {
			return true;
		} else {
			return false;
		}
	});
	file_copy(`${nm}/bootstrap-table/dist/bootstrap-table.min.css`, `${dst}bootstrap-table/bootstrap-table.min.css`);
	file_copy(`${nm}/bootstrap-table/dist/bootstrap-table.min.js`, `${dst}bootstrap-table/bootstrap-table.min.js`);
	dir_copy(`${nm}/node-forge/dist`, `${dst}node-forge`, (src) => {
		if (fs.lstatSync(src).isDirectory() || src.includes(`forge.min.js`)) {
			return true;
		} else {
			return false;
		}
	});
	dir_copy(`${nm}/jquery/dist`, `${dst}jquery`, (src) => {
		if (fs.lstatSync(src).isDirectory() || src.includes(`jquery.min`)) {
			// console.log(`T:` + src);
			return true;
		} else {
			// console.log(`F:` + src);
			return false;
		}
	});
	file_copy(`${nm}/jquery-validation/dist/jquery.validate.min.js`, `${dst}jquery-validation/jquery.validate.min.js`);
	file_copy(`${nm}/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js`, `${dst}jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js`);
	dir_copy(`${nm}/blueimp-gallery/img`, `${dst}blueimp-gallery/img`);
	dir_copy(`${nm}/blueimp-gallery/css`, `${dst}blueimp-gallery/css`, (src) => {
		if (fs.lstatSync(src).isDirectory() || src.includes(`blueimp-gallery.min.css`)) {
			return true;
		} else {
			return false;
		}
	});
	dir_copy(`${nm}/blueimp-gallery/js`, `${dst}blueimp-gallery/js`, (src) => {
		if (fs.lstatSync(src).isDirectory() || src.includes(`${path.sep}blueimp-gallery.min.js`)) {
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
	dir_copy(`${nm}/@microsoft/signalr/dist/browser`, `${dst}signalr/browser`, (src) => {
		if (fs.lstatSync(src).isDirectory() || src.includes(`signalr.min.js`)) {
			// console.log(`T:` + src);
			return true;
		} else {
			// console.log(`F:` + src);
			return false;
		}
	});
	dir_copy(`${nm}/@microsoft/signalr-protocol-msgpack/dist/browser`, `${dst}signalr-protocol-msgpack/browser`, (src) => {
		if (fs.lstatSync(src).isDirectory() || src.includes(`signalr-protocol-msgpack.min.js`)) {
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
	dir_copy(`${nm}/chance/dist`, `${dst}chance`);

	await Promise.all(copy_promises);

	return cb();
};

///
/// Main entry point
///
exports.default = gulp.series(
	exports.clean,
	exports.webpack,
	exports.min
);
