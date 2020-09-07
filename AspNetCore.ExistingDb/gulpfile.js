/*global require, __dirname*/
"use strict";

const gulp = require("gulp"),
	rimraf = require("rimraf"),
	concat = require("gulp-concat"),
	cleanCSS = require("gulp-clean-css"),
	terser = require("gulp-terser"),
	//babel = require("gulp-babel"),
	rename = require("gulp-rename"),
	path = require('path'),
	webpack = require('webpack-stream'),
	EsmWebpackPlugin = require("@purtuga/esm-webpack-plugin");

var webroot = "./wwwroot/";

var paths = {
	js: webroot + "js/**/*.js",
	minJs: webroot + "js/**/*.min.js",
	css: webroot + "css/**/*.css",
	minCss: webroot + "css/**/*.min.css",
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
	concatCssDest: webroot + "css/site.min.css",
	inkBallJsRelative: "../InkBall/src/InkBall.Module/wwwroot/js/",
	inkBallCssRelative: "../InkBall/src/InkBall.Module/wwwroot/css/"
};

////////////// [Inkball Section] //////////////////
const babelTranspilerFunction = function (min) {
	return gulp.src([
		paths.inkBallJsRelative + 'inkball.js'
		//paths.inkBallJsRelative + 'svgvml.js',
		//paths.inkBallJsRelative + 'concavemanSource.js'
	]).pipe(webpack({
		resolve: {
			modules: ['node_modules', `../../../../../${path.basename(__dirname)}/node_modules`]
		},
		entry: {
			'inkball': paths.inkBallJsRelative + 'inkball.js'
			//, 'svgvml.babelify': paths.inkBallJsRelative + 'svgvml.js',
			//, 'concaveman': paths.inkBallJsRelative + 'concavemanSource.js'
		},
		output: {
			filename: '[name]Bundle.js',
			chunkFilename: '[name]Bundle.js',
			publicPath: '../js/'
		},
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
		},
		mode: "production",
		stats: "errors-warnings"
	}))
		.pipe(rename({ suffix: min ? '.min' : '' }))
		.pipe(gulp.dest(paths.inkBallJsRelative));
};

const fileMinifyJSFunction = function (src, result) {
	return gulp.src([src, "!" + result], { base: "." })
		.pipe(concat(result))
		.pipe(terser())
		.pipe(gulp.dest("."));
};

const fileMinifyCSSFunction = function (src, result) {
	return gulp.src([src, "!" + result], { base: "." })
		.pipe(concat(result))
		.pipe(cleanCSS())
		.pipe(gulp.dest("."));
};

gulp.task('webpack:inkballConcaveMan', function () {
	return gulp.src(paths.inkBallJsRelative + "concavemanSource.js")
		.pipe(webpack({
			resolve: {
				modules: ['node_modules', `../../../../../${path.basename(__dirname)}/node_modules`],
				alias: {
					'tinyqueue': 'tinyqueue/tinyqueue.js' //https://github.com/mapbox/concaveman/issues/18
				}
			},
			output: {
				filename: 'concavemanBundle.js',
				library: 'concavemanBundle' //add this line to enable re-use
			},
			plugins: [
				new EsmWebpackPlugin()
			],
			optimization: {
				minimize: true
			},
			mode: "production",
			stats: "errors-warnings"
		}))
		.pipe(gulp.dest(paths.inkBallJsRelative));
});

gulp.task("babel", gulp.series("webpack:inkballConcaveMan", function transpilers(cb) {
	babelTranspilerFunction(false);
	babelTranspilerFunction(true);
	return cb();
}));

gulp.task("min:inkball", gulp.parallel(function inkballJs() {
	return fileMinifyJSFunction(paths.inkBallJsRelative + "inkball.js",
		paths.inkBallJsRelative + "inkball.min.js");
},
	function inkballSvgVmlJs() {
		return fileMinifyJSFunction(paths.inkBallJsRelative + "svgvml.js",
			paths.inkBallJsRelative + "svgvml.min.js");
	},
	function inkballCSSMinify() {
		return fileMinifyCSSFunction(paths.inkBallCssRelative + "inkball.css",
			paths.inkBallCssRelative + "inkball.min.css");
	}));

gulp.task("clean:inkball", function (cb) {
	rimraf(paths.inkBallJsRelative + "*.min.js", cb);
	rimraf(paths.inkBallJsRelative + "*.babelify*", cb);
	rimraf(paths.inkBallCssRelative + "*.min.css", cb);
	rimraf(paths.inkBallJsRelative + "*Bundle.js", cb);
});
////////////// [/Inkball Section] //////////////////

gulp.task("clean:js", gulp.series("clean:inkball", function cleanConcatJsDest(cb) {
	rimraf(paths.concatJsDest, cb);
	rimraf(paths.SWJsDest, cb);
	rimraf(paths.BruteForceWorkerJsDest, cb);
	rimraf(paths.SharedJsDest, cb);
}));

gulp.task("clean:css", function (cb) {
	rimraf(paths.concatCssDest, cb);
});

gulp.task("clean", gulp.series("clean:js", "clean:css"));

gulp.task("minSWJs:js", function () {
	return fileMinifyJSFunction(paths.SWJs, paths.SWJsDest);
});

gulp.task("minBruteForceWorker:js", function () {
	return fileMinifyJSFunction(paths.BruteForceWorkerJs, paths.BruteForceWorkerJsDest);
});

gulp.task("Shared:js", function () {
	return fileMinifyJSFunction(paths.SharedJs, paths.SharedJsDest);
});

gulp.task("min:js", gulp.series("minSWJs:js", "minBruteForceWorker:js", "Shared:js",
	function concatJsDest() {
		return gulp.src([paths.js, "!" + paths.minJs], { base: "." })
			.pipe(concat(paths.concatJsDest))
			.pipe(terser())
			.pipe(gulp.dest("."));
	}
));

gulp.task("min:css", function () {
	return gulp.src([paths.css, "!" + paths.minCss])
		.pipe(concat(paths.concatCssDest))
		.pipe(cleanCSS())
		.pipe(gulp.dest("."));
});

gulp.task("min", gulp.parallel("min:js", "min:inkball", "min:css"));

//Main entry point
gulp.task("default", gulp.series(
	"clean",
	"babel", "min")
);
