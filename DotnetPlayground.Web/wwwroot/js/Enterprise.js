window.addEventListener('load', () => {

	function getQueryParam(name) {
		// Check regular query string: ?a=1&authcodecallback=...
		const params = new URLSearchParams(window.location.search);
		if (params.has(name)) return params.get(name);

		// Check hash fragment for embedded query string or fragment parameters:
		// Examples: #/path?authcodecallback=...  or #authcodecallback=...
		const hash = window.location.hash || "";
		// If there's a '?' in the hash, parse the substring starting from '?'
		const qIndex = hash.indexOf("?");
		if (qIndex !== -1) {
			const hashQuery = new URLSearchParams(hash.substring(qIndex));
			if (hashQuery.has(name)) return hashQuery.get(name);
		}

		// Also support simple hash key=value pairs like #authcodecallback=...
		if (hash.indexOf(name + "=") !== -1 && qIndex === -1) {
			// Remove leading '#' then split
			const hashNoHash = hash.charAt(0) === "#" ? hash.substring(1) : hash;
			const pairs = hashNoHash.split("&");
			for (let i = 0; i < pairs.length; i++) {
				const kv = pairs[i].split("=");
				if (kv[0] === name) return decodeURIComponent(kv[1] || "");
			}
		}

		return null;
	}

	// Validate that a URL is safe to redirect to (protocol and origin checks).
	function isSafeRedirectUrl(urlObj) {
		// Only allow http and https protocols
		if (urlObj.protocol !== "http:" && urlObj.protocol !== "https:") {
			return false;
		}
		// Optionally restrict to same origin to prevent open redirects
		try {
			if (urlObj.origin !== window.location.origin) {
				return false;
			}
		} catch (e) {
			return false;
		}
		return true;
	}

	// Unicode-safe base64 encoder
	function base64EncodeUnicode(str) {
		const bytes = new TextEncoder().encode(str);
		let binary = "";
		for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);
		return btoa(binary);
	}
	const el = $("#authcodecallback");
	if (!el) return;

	const value = getQueryParam("authcodecallback");
	if (value) {
		//localStorage.setItem("authcodecallback", value);
		// Remove all query params from the URL and reload without them
		const acc = new URL(value);

		// Ensure the callback URL is safe before using it
		if (!isSafeRedirectUrl(acc)) {
			$("#submitBtn").hide();
			return;
		}

		// generate a UUID (use crypto.randomUUID)
		const guid = crypto.randomUUID();
		const userPlusRand = $('#userName').text() + '.' + guid;
		const base64 = base64EncodeUnicode(userPlusRand);

		acc.searchParams.set("code", base64.toString());
		el.val(acc.href);
		//add submitBtn click handler with jquery
		$("#submitBtn").show().on("click", function () {
			const el = $("#authcodecallback");
			window.location.href = el.val();
		});
	} else {
		$("#submitBtn").hide();
	}
});
