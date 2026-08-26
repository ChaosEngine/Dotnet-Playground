"use strict";
import process from 'node:process';
import fs from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

import * as dartSass from 'sass';
import webpack from 'webpack';
import { minify as terserMinify } from 'terser';
import CleanCSS from 'clean-css';

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

/**
 * Parses command line arguments.
 * @returns {{ task: string, flags: Record<string, string> }} - task and flags object
 */
const parseCliArgs = () => {
	const args = process.argv.slice(2);
	let task = 'main';
	const flags = {};

	for (const arg of args) {
		if (arg.startsWith('--')) {
			const [rawKey, ...rest] = arg.slice(2).split('=');
			flags[rawKey] = rest.length > 0 ? rest.join('=') : 'true';
		} else if (task === 'main') {
			task = arg;
		}
	}

	if (typeof flags.task === 'string' && flags.task.length > 0) {
		task = flags.task;
	}

	return { task, flags };
};

const cli = parseCliArgs();

/**
 * Converts a glob pattern to a regular expression.
 * @param {string} pattern - The glob pattern
 * @returns {RegExp} - The corresponding regular expression
 */
const globToRegex = (pattern) => {
	const normalizedPattern = pattern.replaceAll('\\\\', '/');
	return new RegExp('^' + normalizedPattern
		.replace(/[.+^${}()|[\]\\]/g, '\\\\$&')
		.replaceAll('**', ':::DOUBLE_STAR:::')
		.replaceAll('*', '[^/]*')
		.replaceAll(':::DOUBLE_STAR:::', '.*') + '$');
};

/**
 * Lists files matching the given patterns.
 * @param {string[]} patterns - Array of glob patterns
 * @returns {Promise<string[]>} - Array of matching file paths
 */
const listFiles = async (patterns) => {
	const include = patterns.filter(x => !x.startsWith('!'));

	const excludeRegexes = patterns
		.filter(x => x.startsWith('!'))
		.map(x => globToRegex(x.substring(1)));

	const batches = await Promise.all(include.map(async (pattern) => {
		const files = [];
		for await (const file of fs.glob(pattern)) {
			if (!excludeRegexes.some(re => re.test(file))) {
				files.push(file);
			}
		}
		return files;
	}));

	return Array.from(new Set(batches.flat()));
};

const ensureParentDir = async (filePath) => {
	await fs.mkdir(path.dirname(filePath), { recursive: true });
};

const writeTextFile = async (filePath, contents) => {
	await ensureParentDir(filePath);
	await fs.writeFile(filePath, contents, 'utf8');
};

const minifyCssFile = async (src, dest) => {
	const css = await fs.readFile(src, 'utf8');
	const result = new CleanCSS({
		sourceMap: true,
		sourceMapInlineSources: false
	}).minify({
		[path.basename(src)]: {
			styles: css
		}
	});
	if (result.errors.length > 0) {
		throw new Error(`CSS minification failed for '${src}': ${result.errors.join('; ')}`);
	}
	let minifiedCss = result.styles;
	const sourceMapComment = `/*# sourceMappingURL=${path.basename(dest)}.map */`;
	if (!minifiedCss.includes('sourceMappingURL=')) {
		minifiedCss = `${minifiedCss}\n${sourceMapComment}\n`;
	}

	await writeTextFile(dest, minifiedCss);
	if (result.sourceMap) {
		const sourceMapJson = result.sourceMap.toJSON();
		sourceMapJson.file = path.basename(dest);
		await writeTextFile(`${dest}.map`, JSON.stringify(sourceMapJson));
	}
};

const minifyJsFile = async (src, dest, toplevel = false) => {
	const code = await fs.readFile(src, 'utf8');
	const sourceName = path.basename(src);
	const result = await terserMinify({
		[sourceName]: code
	}, {
		toplevel,
		sourceMap: {
			filename: path.basename(dest),
			url: `${path.basename(dest)}.map`
		}
	});

	if (!result.code) {
		throw new Error(`JS minification failed for '${src}'`);
	}

	await writeTextFile(dest, result.code + '\n');
	if (result.map) {
		await writeTextFile(`${dest}.map`, result.map);
	}
};

const minifyJsonFile = async (src, dest) => {
	const raw = await fs.readFile(src, 'utf8');
	const minified = JSON.stringify(JSON.parse(raw));
	await writeTextFile(dest, minified);
};

const compileScssFile = async (src, dest, replacer) => {
	const rawScss = await fs.readFile(src, 'utf8');
	const transformed = replacer ? replacer(rawScss) : rawScss;
	const compiled = await dartSass.compileStringAsync(transformed, {
		style: 'expanded',
		url: pathToFileURL(path.resolve(src))
	});
	await writeTextFile(dest, compiled.css);
	// compiled.sourceMap ????
};

const runWebpack = (config) => {
	return new Promise((resolve, reject) => {
		webpack(config, (err, stats) => {
			if (err) {
				reject(err);
				return;
			}
			if (stats?.hasErrors()) {
				reject(new Error(stats.toString('errors-warnings')));
				return;
			}
			resolve(stats);
		});
	});
};

/**
 * Deletes files matching the provided glob patterns.
 * @param {Array<string>} globPatterns array of glob patterns
 * @returns {Promise<void[]>} Promise that resolves when all files are deleted
 */
const rimraf = async function (globPatterns) {
	const found_files = new Set();
	for (const pattern of globPatterns) {
		for await (const file of fs.glob(pattern))
			found_files.add(file);
	}
	if (found_files.size === 0)
		return Promise.resolve();

	return Promise.all(Array.from(found_files).map(file => {
		// console.log(file);
		return fs.rm(file, { force: true });
	}));
};

////////////// [Inkball Section] //////////////////
// eslint-disable-next-line no-unused-vars
const inkballEntryPoint = async function (min) {
	await runWebpack({
		entry: {
			'inkball': [
				//'@babel/polyfill',
				path.resolve(paths.inkBallJsRelative, 'inkball.js')
			]
		},
		output: {
			filename: '[name].Bundle.js',
			chunkFilename: '[name].Bundle.js',
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
		performance: {
			hints: process.env.NODE_ENV === 'production' ? "warning" : false
		},
		mode: 'production',
		stats: 'errors-warnings',
		devtool: 'source-map'
	});
};

const inkballAIWorker = async function (doPollyfill = false) {
	await runWebpack({
		entry: {
			AIWorker: doPollyfill === true ? [
				'@babel/polyfill',
				path.resolve(paths.inkBallJsRelative, 'AIWorker.js')
			] : [
				path.resolve(paths.inkBallJsRelative, 'AIWorker.js')
			]
		},
		// target: "webworker",
		output: {
			path: path.resolve(paths.inkBallJsRelative),
			filename: doPollyfill === true ? '[name].PolyfillBundle.js' : '[name].Bundle.js'
		},
		module: doPollyfill === true ? {
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
		} : {},
		optimization: {
			minimize: true
		},
		performance: {
			hints: process.env.NODE_ENV === 'production' ? 'warning' : false
		},
		mode: 'production',
		stats: 'errors-warnings',
		devtool: 'source-map'
	});
};

const webpackRun = inkballAIWorker;

const minInkballJs = async function () {
	await Promise.all([
		minifyJsFile(paths.inkBallJsRelative + 'inkball.js', paths.inkBallJsRelative + 'inkball.min.js', true),
		minifyJsFile(paths.inkBallJsRelative + 'shared.js', paths.inkBallJsRelative + 'shared.min.js', true)
	]);
};

const minInkballCss = async function () {
	await compileScssFile(paths.inkBallCssRelative + 'inkball.scss', paths.inkBallCssRelative + 'inkball.css');
	await minifyCssFile(paths.inkBallCssRelative + 'inkball.css', paths.inkBallCssRelative + 'inkball.min.css');
};

const minInkballTranslations = async function () {
	const files = await listFiles([paths.inkBallTranslation, '!' + paths.inkBallMinTranslation]);
	await Promise.all(files.map(file => {
		const dest = file.replace(/\.json$/i, '.min.json');
		return minifyJsonFile(file, dest);
	}));
};

const minInkball = async function () {
	await Promise.all([minInkballJs(), minInkballCss(), minInkballTranslations()]);
};

const clean = async function () {
	await rimraf([
		paths.inkBallJsRelative + '*.min.js',
		paths.inkBallJsRelative + '*Bundle.js',
		paths.inkBallJsRelative + '*.map',
		paths.inkBallMinTranslation
		,
		paths.minJs,
		paths.SWJsDest,
		paths.minTranslation,
		webroot + '**/*.map'
		,
		paths.inkBallCssRelative + '*.css',
		paths.inkBallCssRelative + '*.map'
		,
		webroot + 'css/*.css'
	]);
};

const minSWJsJs = async function () {
	await minifyJsFile(paths.SWJs, paths.SWJsDest);
};

const minJs = async function () {
	await minSWJsJs();
	const jsFiles = await listFiles([paths.js, '!' + paths.minJs]);
	await Promise.all(jsFiles.map(file => {
		const dest = file.replace(/\.js$/i, '.min.js');
		return minifyJsFile(file, dest);
	}));
};

const minTranslations = async function () {
	const files = await listFiles([paths.translation, '!' + paths.minTranslation]);
	await Promise.all(files.map(file => {
		const dest = file.replace(/\.json$/i, '.min.json');
		return minifyJsonFile(file, dest);
	}));
};

const processInputArgs = function () {
	let colorTheme;//process.env.NODE_ENV === 'production' ? 'darkred' : 'darkslateblue';
	let env = undefined;
	let projectVersion = undefined;

	if (typeof cli.flags.version === 'string' && cli.flags.version.length > 0)
		projectVersion = cli.flags.version;
	if (typeof cli.flags.env === 'string' && cli.flags.env.length > 0)
		env = cli.flags.env;

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

const processSCSS = async function (sourcePattern, notPattern) {
	const { colorTheme, projectVersion } = processInputArgs();
	const files = await listFiles([sourcePattern]);

	await Promise.all(files.map(file => {
		const outFile = path.join(notPattern, path.basename(file).replace(/\.scss$/i, '.css'));
		return compileScssFile(file, outFile, input =>
			input
				.replaceAll('$themeColor', colorTheme)
				.replaceAll('$projectVersion', `'${projectVersion}'`)
		);
	}));
};

const minScss = async function () {
	await processSCSS(paths.scss, paths.destCSSDir);
	await minifyCssFile(webroot + 'css/site.css', webroot + 'css/site.min.css');
	await minifyCssFile(webroot + 'css/icons.css', webroot + 'css/icons.min.css');
};

const min = async function () {
	await Promise.all([minJs(), minInkball(), minScss(), minTranslations()]);
};

const cssRun = async function () {
	await Promise.all([minInkballCss(), minScss()]);
};

///
/// postinstall entry point (npm i)
///
const postinstall = () => {
	const copy_promises = [];
	const file_copy = async (src, dst) => {
		// await ensureParentDir(dst);
		copy_promises.push(fs.cp(src, dst));
	};
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

	dir_copy(`${nm}/blueimp-md5/js`, `${dst}blueimp-md5`, async (src) => {
		if ((await fs.lstat(src)).isDirectory() || src.includes(`md5.min.js`)) {
			return true;
		} else {
			return false;
		}
	});
	file_copy(`${nm}/jquery/dist/jquery.min.js`, `${dst}jquery/jquery.min.js`);

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
			return true;
		} else {
			return false;
		}
	});
	file_copy(`${nm}/video.js/dist/video-js.min.css`, `${dst}video.js/video-js.min.css`);
	file_copy(`${nm}/video.js/dist/alt/video.core.novtt.min.js`, `${dst}video.js/alt/video.core.novtt.min.js`);
	file_copy(`${nm}/qrcodejs/qrcode.min.js`, `${dst}qrcodejs/qrcode.min.js`);
	dir_copy(`${nm}/@microsoft/signalr/dist/browser`, `${dst}signalr/browser`, async (src) => {
		if ((await fs.lstat(src)).isDirectory() || src.includes(`signalr.min.js`)) {
			return true;
		} else {
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

	return Promise.all(copy_promises);
};

///
/// Main entry point
///
const main = async function () {
	await clean();
	// await webpackRun();
	// await min();
	await Promise.all([webpackRun(), min()]);
};

const tasks = {
	main,
	clean,
	webpack: webpackRun,
	min,
	css: cssRun,
	postinstall
};

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) {
	const selectedTask = tasks[cli.task];
	if (!selectedTask) {
		// eslint-disable-next-line no-console
		console.error(`Unknown task '${cli.task}'. Available tasks: ${Object.keys(tasks).join(', ')}`);
		process.exitCode = 1;
	} else {
		selectedTask().catch(err => {
			// eslint-disable-next-line no-console
			console.error(err);
			process.exitCode = 1;
		});
	}
}