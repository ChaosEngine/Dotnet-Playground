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
	inkBallJsRelative: "../InkBall/src/InkBall.Module/wwwroot/js/",
	inkBallCssRelative: "../InkBall/src/InkBall.Module/wwwroot/css/"
};

const minCSS = function (sourcePattern, notPattern, dest) {
	return gulp.src([sourcePattern, "!" + notPattern])
		.pipe(concat(dest))
		.pipe(cleanCSS())
		.pipe(gulp.dest("."));
};

////////////// [Inkball Section] //////////////////
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
			stats: "errors-warnings"
		}))
		.pipe(gulp.dest(paths.inkBallJsRelative));
};

exports.webpack = gulp.parallel(function inkballWebWorkerEntryPoint(cb) {
	inkballAIWorker(true);
	inkballAIWorker(false);
	return cb();
}, function inkballMainEntryPoint(cb) {
	inkballEntryPoint(false);
	inkballEntryPoint(true);
	return cb();
});

const fileMinifyJSFunction = function (src, result, toplevel = false) {
	return gulp.src([src, "!" + result], { base: "." })
		.pipe(concat(result))
		.pipe(terser({
			toplevel: toplevel
		}))
		.pipe(gulp.dest("."));
};

const fileMinifySCSSFunction = function (src, result) {
	return gulp.src([src, "!" + result], { base: "." })
		.pipe(concat(result))
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
	rimraf(paths.inkBallJsRelative + "*.babelify*", cb);
	rimraf(paths.inkBallCssRelative + "*.css", cb);
	rimraf(paths.inkBallJsRelative + "*Bundle.js", cb);
};
////////////// [/Inkball Section] //////////////////

const cleanJs = gulp.series(cleanInkball, function cleanMinJs(cb) {
	rimraf(paths.minJs, cb);
	rimraf(paths.SWJsDest, cb);
});

const cleanCss = function (cb) {
	rimraf(paths.concatCssDest, cb);
	rimraf(paths.concatCssDestMin, cb);
};

exports.clean = gulp.series(cleanJs, cleanCss);

const minSWJsJs = function () {
	return fileMinifyJSFunction(paths.SWJs, paths.SWJsDest);
};

const minJs = gulp.series(minSWJsJs,
	function concatJsDest() {
		return gulp.src([paths.js, "!" + paths.minJs], { base: "." })
			//.pipe(concat(paths.concatJsDest))
			.pipe(terser())
			.pipe(rename({ suffix: '.min' }))
			.pipe(gulp.dest("."));
	}
);

const minCss = function runTaskMinCSS() {
	return minCSS(paths.css, paths.minCss, paths.concatCssDestMin);
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
		.pipe(header('$themeColor: ${color};\n$projectVersion: ${version};\n', { color: colorTheme, version: `'${projectVersion}'` }))
		.pipe(sass().on('error', sass.logError))
		.pipe(gulp.dest(notPattern));
};

const minScss = gulp.series(function scssToCss() {
	return processSCSS(paths.scss, paths.destCSSDir);
}, minCss);

exports.min = gulp.parallel(minJs, minInkball, minScss);


gulp.task("min", gulp.parallel("min:js", "min:inkball", "min:scss"));

///
/// Main entry point
///
exports.default = gulp.series(
	exports.clean,
	exports.webpack,
	exports.min
);
