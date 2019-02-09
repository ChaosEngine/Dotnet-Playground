/// <binding Clean='clean' />
/*global require*/
"use strict";

const gulp = require("gulp"),
	//sass = require("gulp-sass"),
	rimraf = require("rimraf"),
	concat = require("gulp-concat"),
	cssmin = require("gulp-cssmin"),
	//uglify = require("gulp-uglify"),
	//gulpBabelMinify = require("gulp-babel-minify"),
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
	return gulp.src('../InkBall/src/InkBall.Module/wwwroot/js/inkball.js')
		.pipe(babel({
			"presets": min ?
				[
					["@babel/preset-env", { "useBuiltIns": "entry" }],
					["minify"]
				]
				:
				[
					["@babel/preset-env", { "useBuiltIns": "entry" }]
				],
			"comments": false
		}))
		.pipe(rename({ suffix: min ? '.babelify.min' : '.babelify' }))
		.pipe(gulp.dest('../InkBall/src/InkBall.Module/wwwroot/js/'));
};

const fileMinifyFunction = function (src, result) {
	return gulp.src([src, "!" + result], { base: "." })
		.pipe(concat(result))
		.pipe(babel({
			"presets": ["minify"], "comments": false
		}))
		.pipe(gulp.dest("."));
};

gulp.task("babel", function(cb) {
	babelTranspilerFunction(false);
	babelTranspilerFunction(true);
	return cb();
});

gulp.task("min:inkball", gulp.parallel(function inkballJs() {
	return fileMinifyFunction("../InkBall/src/InkBall.Module/wwwroot/js/inkball.js",
		"../InkBall/src/InkBall.Module/wwwroot/js/inkball.min.js");
},
	function inkballSvgVmlJs() {
		return fileMinifyFunction("../InkBall/src/InkBall.Module/wwwroot/js/svgvml.js",
			"../InkBall/src/InkBall.Module/wwwroot/js/svgvml.min.js");
	}));

gulp.task("clean:inkball", function (cb) {
	rimraf("../InkBall/src/InkBall.Module/wwwroot/js/*.min.js", cb);
	rimraf("../InkBall/src/InkBall.Module/wwwroot/js/inkball.babelify*", cb);
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
		//.pipe(cssmin())
		.pipe(gulp.dest(webroot + "lib/bootstrap/dist/css"));
});*/

gulp.task("min:js", function () {
	return gulp.src([paths.js, "!" + paths.minJs], { base: "." })
		.pipe(concat(paths.concatJsDest))
		.pipe(babel({
			"presets": ["minify"], "comments": false
		}))
		.pipe(gulp.dest("."));
});

gulp.task("min:css", function () {
	return gulp.src([paths.css, "!" + paths.minCss])
		.pipe(concat(paths.concatCssDest))
		.pipe(cssmin())
		.pipe(gulp.dest("."));
});

gulp.task("min", gulp.parallel("min:js", "min:inkball", "min:css"));

//Main entry point
gulp.task("default", gulp.series(
	"clean",
	//"scss",
	"babel", "min")
);
