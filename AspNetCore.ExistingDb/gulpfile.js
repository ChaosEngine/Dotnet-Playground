/// <binding Clean='clean' />
"use strict";

var gulp = require("gulp"),
	less = require("gulp-less"),
    rimraf = require("rimraf"),
    concat = require("gulp-concat"),
    cssmin = require("gulp-cssmin"),
    uglify = require("gulp-uglify");

var webroot = "./wwwroot/";

var paths = {
    js: webroot + "js/**/*.js",
    minJs: webroot + "js/**/*.min.js",
    css: webroot + "css/**/*.css",
    minCss: webroot + "css/**/*.min.css",
    concatJsDest: webroot + "js/site.min.js",
	concatCssDest: webroot + "css/site.min.css",
	boostrapLess: webroot + "css/bootstrap.less"
};

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

gulp.task("MyBootstapColors:css", function () {
	return gulp.src([paths.boostrapLess])
		.pipe(less())
		.pipe(cssmin())
		.pipe(gulp.dest(webroot + "lib/bootstrap/dist/css"));
});

gulp.task("min:css", function () {
	return gulp.src([paths.css, "!" + paths.minCss])
        .pipe(concat(paths.concatCssDest))
		.pipe(cssmin())
        .pipe(gulp.dest("."));
});

gulp.task("min", gulp.series("min:js",
	//"MyBootstapColors:css",
	"min:css"));
