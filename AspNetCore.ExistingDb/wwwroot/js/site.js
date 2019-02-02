/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "clientValidate" }]*/
/*global g_AppRootPath forge*/
var logLevel = {
	Trace: 0,
	Debug: 1,
	Information: 2,
	Warning: 3,
	Error: 4
};

var g_LogPath = g_AppRootPath + "Home/ClientsideLog";

function ajaxLog(level, message, url, line, col, error) {
	$.post(g_LogPath, {
		"level": level, "message": message, "url": url, "line": line, "col": col, "error": error
	});
}

function clientValidate(button) {
	var tr = $(button).parent().parent();

	var key = tr.find("td").eq(0).text();
	var orig_md5 = tr.find("td").eq(1).text();
	var orig_sha = tr.find("td").eq(2).text();

	if (orig_md5 === '' || orig_sha === '')
		return;

	var md = forge.md.md5.create();
	md.update(key);
	let md5 = md.digest().toHex();
	md = forge.md.sha256.create();
	md.update(key);
	let sha = md.digest().toHex();

	tr.find("td").eq(1).html("<strong style='color:" + (md5 === orig_md5 ? "green" : "red") + "'>" + orig_md5 + "</strong>");
	tr.find("td").eq(2).html("<strong style='color:" + (sha === orig_sha ? "green" : "red") + "'>" + orig_sha + "</strong>");
}

(function () {
	var org_trace = console.trace;
	var org_debug = console.debug;
	var org_info = console.info;
	var org_warn = console.warn;
	var org_error = console.error;

	console.trace = function (message) {
		ajaxLog(logLevel.Trace, message);
		org_trace.call(this, arguments);
	};

	console.debug = function (message) {
		ajaxLog(logLevel.Debug, message);
		org_debug.call(this, arguments);
	};

	console.info = function (message) {
		ajaxLog(logLevel.Information, message);
		org_info.call(this, arguments);
	};

	console.warn = function (message) {
		ajaxLog(logLevel.Warning, message);
		org_warn.call(this, arguments);
	};

	console.error = function (msg, url, line, col, error) {
		ajaxLog(logLevel.Error, msg, url, line, col, error);
		org_error.call(this, arguments);
	};
})();

window.onerror = function (msg, url, line, col, error) {
	// Note that col & error are new to the HTML 5 spec and may not be 
	// supported in every browser.  It worked for me in Chrome.
	//var extra = !col ? '' : ('\ncolumn: ' + col);
	//extra += !error ? '' : ('\nerror: ' + error);

	// You can view the information in an alert to see things working like this:
	//alert("Error: " + msg + "\nurl: " + url + "\nline: " + line + extra);
	console.error(msg, url, line, col, error);

	var suppressErrorAlert = true;
	// If you return true, then error alerts (like in older versions of 
	// Internet Explorer) will be suppressed.
	return suppressErrorAlert;
};

//
// https://stackoverflow.com/a/2641047/4429828
//
$.fn.bindFirst = function (name, fn) {
	// Bind as you normally would. Don't want to miss out on any jQuery magic
	this.on(name, fn);

	// Thanks to a comment by @@Martin, adding support for namespaced events too.
	this.each(function () {
		var handlers = $._data(this, 'events')[name.split('.')[0]];
		//console.log(handlers);
		// take out the handler we just inserted from the end
		var handler = handlers.pop();
		// move it at the beginning
		handlers.splice(0, 0, handler);
	});
};
