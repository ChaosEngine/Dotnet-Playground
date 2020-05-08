/*global require, __dirname*/
"use strict";

const gulp = require("gulp"),
	//sass = require("gulp-sass"),
	rimraf = require("rimraf"),
	concat = require("gulp-concat"),
	cleanCSS = require("gulp-clean-css"),
	terser = require("gulp-terser"),
	babel = require("gulp-babel"),
	rename = require("gulp-rename"),
	path = require('path'),
	webpack = require('webpack-stream');

var webroot = "./wwwroot/";

var paths = {
	js: webroot + "js/**/*.js",
	minJs: webroot + "js/**/*.min.js",
	css: webroot + "css/**/*.css",
	minCss: webroot + "css/**/*.min.css",
	concatJsDest: webroot + "js/site.min.js",
	SWJs: webroot + "sw.js",
	SWJsDest: webroot + "sw.min.js",
	concatCssDest: webroot + "css/site.min.css",
	inkBallJsRelative: "../InkBall/src/InkBall.Module/wwwroot/js/",
	inkBallCssRelative: "../InkBall/src/InkBall.Module/wwwroot/css/"
	//boostrapSass: webroot + "css/bootstrap.scss"
};

////////////// [Inkball Section] //////////////////
const babelTranspilerFunction = function (min) {
	let tunnel = gulp.src([
		paths.inkBallJsRelative + 'inkball.js',
		paths.inkBallJsRelative + 'svgvml.js'
	]).pipe(babel({
		"presets":
			[
				["@babel/preset-env", { "useBuiltIns": "entry", "corejs": 3 }]
			],
		"comments": false
	}));

	if (min)
		tunnel = tunnel.pipe(terser());

	return tunnel.pipe(rename({ suffix: min ? '.babelify.min' : '.babelify' }))
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

gulp.task("babel", function (cb) {
	babelTranspilerFunction(false);
	babelTranspilerFunction(true);
	return cb();
});

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
	rimraf(paths.inkBallJsRelative + "inkball.babelify*", cb);
	rimraf(paths.inkBallCssRelative + "*.min.css", cb);
	rimraf(paths.inkBallJsRelative + "concavemanBundle.js", cb);
});
gulp.task('webpack:inkballConcaveMan', function () {
	const rootPath = path.basename(__dirname);
	//console.log('ch_ __dirname = ' + rootPath);
	return gulp.src(paths.inkBallJsRelative + "concavemanSource.js")
		.pipe(webpack({
			resolve: {
				modules: ['node_modules', `../../../../../${rootPath}/node_modules`]
			},
			output: {
				filename: 'concavemanBundle.js',
				library: 'concavemanBundle' //add this line to enable re-use
			},
			optimization: {
				minimize: true
			},
			mode: "production",
			stats: "errors-warnings"
		}))
		.pipe(gulp.dest(paths.inkBallJsRelative));
});
////////////// [/Inkball Section] //////////////////

gulp.task("clean:js", gulp.series("clean:inkball", function cleanConcatJsDest(cb) {
	rimraf(paths.concatJsDest, cb);
	rimraf(paths.SWJsDest, cb);
}));

gulp.task("clean:css", function (cb) {
	rimraf(paths.concatCssDest, cb);
});

gulp.task("clean", gulp.series("clean:js", "clean:css"));

/*gulp.task("scss", function () {
	return gulp.src([paths.boostrapSass])
		.pipe(sass().on('error', sass.logError))
		//.pipe(cleanCSS())
		.pipe(gulp.dest(webroot + "lib/bootstrap/dist/css"));
});*/

gulp.task("minSWJs:js", function () {
	return fileMinifyJSFunction(paths.SWJs, paths.SWJsDest);
});

gulp.task("min:js", gulp.series("minSWJs:js", function concatJsDest() {
	return gulp.src([paths.js, "!" + paths.minJs], { base: "." })
		.pipe(concat(paths.concatJsDest))
		.pipe(terser())
		.pipe(gulp.dest("."));
}));

gulp.task("min:css", function () {
	return gulp.src([paths.css, "!" + paths.minCss])
		.pipe(concat(paths.concatCssDest))
		.pipe(cleanCSS())
		.pipe(gulp.dest("."));
});

gulp.task("min", gulp.parallel("min:js", "min:inkball", "min:css", "webpack:inkballConcaveMan"));

//Main entry point
gulp.task("default", gulp.series(
	"clean",
	//"scss",
	"babel", "min")
);
