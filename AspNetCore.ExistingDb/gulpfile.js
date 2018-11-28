﻿/// <binding Clean='clean' />
"use strict";

var gulp = require("gulp"),
	//sass = require("gulp-sass"),
	rimraf = require("rimraf"),
	concat = require("gulp-concat"),
	cssmin = require("gulp-cssmin"),
	uglify = require("gulp-uglify"),
	babel = require("gulp-babel"),
	rename = require("gulp-rename");

var webroot = "./wwwroot/";

var paths = {
	js: webroot + "js/**/*.js",
	minJs: webroot + "js/**/*.min.js",
	css: webroot + "css/**/*.css",
	minCss: webroot + "css/**/*.min.css",
	concatJsDest: webroot + "js/site.min.js",
	concatCssDest: webroot + "css/site.min.css",
	boostrapSass: webroot + "css/bootstrap.scss"
};

gulp.task("babel", function () {
	return gulp.src(
		[
			'../InkBall/src/InkBall.Module/wwwroot/js/inkball.js'
		])
		.pipe(babel({
			"presets": [
				[
					"@babel/preset-env",
					{
						"useBuiltIns": "entry"
					}
				]
			]
		}))
		.pipe(rename({ suffix: '.babelify' }))
		.pipe(gulp.dest('../InkBall/src/InkBall.Module/wwwroot/js/'));
});

gulp.task("clean:js", function (cb) {
	rimraf(paths.concatJsDest, cb);
});

gulp.task("clean:css", function (cb) {
	rimraf(paths.concatCssDest, cb);
});

gulp.task("clean", gulp.series("clean:js", "clean:css"));

gulp.task("min:js", function () {
	return gulp.src([paths.js, "!" + paths.minJs], { base: "." })
		.pipe(concat(paths.concatJsDest))
		.pipe(uglify())
		.pipe(gulp.dest("."));
});

gulp.task("MyBootstrapColors:scss", function () {
	return gulp.src([paths.boostrapSass])
		.pipe(sass().on('error', sass.logError))
		//.pipe(cssmin())
		.pipe(gulp.dest(webroot + "lib/bootstrap/dist/css"));
});

gulp.task("min:css", function () {
	return gulp.src([paths.css, "!" + paths.minCss])
		.pipe(concat(paths.concatCssDest))
		.pipe(cssmin())
		.pipe(gulp.dest("."));
});

gulp.task("min", gulp.series("min:js",
	//"MyBootstrapColors:scss",
	"babel",
	"min:css"));
