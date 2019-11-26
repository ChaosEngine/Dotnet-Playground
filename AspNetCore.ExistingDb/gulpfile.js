/// <binding Clean='clean' />
/*global require*/
"use strict";

const gulp = require("gulp"),
	//sass = require("gulp-sass"),
	rimraf = require("rimraf"),
	concat = require("gulp-concat"),
	cleanCSS = require("gulp-clean-css"),
	terser = require("gulp-terser"),
	babel = require("gulp-babel"),
	rename = require("gulp-rename");

var webroot = "./wwwroot/";

var paths = {
	js: webroot + "js/**/*.js",
	minJs: webroot + "js/**/*.min.js",
	css: webroot + "css/**/*.css",
	minCss: webroot + "css/**/*.min.css",
	concatJsDest: webroot + "js/site.min.js",
	concatCssDest: webroot + "css/site.min.css"
	//boostrapSass: webroot + "css/bootstrap.scss"
};

////////////// [Inkball Section] //////////////////
const babelTranspilerFunction = function (min) {
	let tunnel = gulp.src('../InkBall/src/InkBall.Module/wwwroot/js/inkball.js')
		.pipe(babel({
			"presets":
				[
					["@babel/preset-env", { "useBuiltIns": "entry", "corejs": 3 }]
				],
			"comments": false
		}));

	if (min)
		tunnel = tunnel.pipe(terser());

	return tunnel.pipe(rename({ suffix: min ? '.babelify.min' : '.babelify' }))
		.pipe(gulp.dest('../InkBall/src/InkBall.Module/wwwroot/js/'));
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
	return fileMinifyJSFunction("../InkBall/src/InkBall.Module/wwwroot/js/inkball.js",
		"../InkBall/src/InkBall.Module/wwwroot/js/inkball.min.js");
},
	function inkballSvgVmlJs() {
		return fileMinifyJSFunction("../InkBall/src/InkBall.Module/wwwroot/js/svgvml.js",
			"../InkBall/src/InkBall.Module/wwwroot/js/svgvml.min.js");
	},
	function inkballCSSMinify() {
		return fileMinifyCSSFunction("../InkBall/src/InkBall.Module/wwwroot/css/inkball.css",
			"../InkBall/src/InkBall.Module/wwwroot/css/inkball.min.css");
	}));

gulp.task("clean:inkball", function (cb) {
	rimraf("../InkBall/src/InkBall.Module/wwwroot/js/*.min.js", cb);
	rimraf("../InkBall/src/InkBall.Module/wwwroot/js/inkball.babelify*", cb);
	rimraf("../InkBall/src/InkBall.Module/wwwroot/css/*.min.css", cb);
});
////////////// [/Inkball Section] //////////////////

gulp.task("clean:js", gulp.series("clean:inkball", function cleanConcatJsDest(cb) {
	rimraf(paths.concatJsDest, cb);
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

gulp.task("min:js", function () {
	return gulp.src([paths.js, "!" + paths.minJs], { base: "." })
		.pipe(concat(paths.concatJsDest))
		.pipe(terser())
		.pipe(gulp.dest("."));
});

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
	//"scss",
	"babel", "min")
);
