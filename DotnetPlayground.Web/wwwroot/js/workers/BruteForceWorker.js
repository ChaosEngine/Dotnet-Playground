/*global LazyProductExp hashExp binaryStringToArrayBufferExp*/
(function (s) {

	function arrayBufferToBinaryStringExp(buffer) {
		let binary = "";
		let bytes = new Uint8Array(buffer);
		let length = bytes.byteLength;
		for (let i = 0; i < length; i++)
			binary += String.fromCharCode(bytes[i]);
		return binary;
	}

	// https://github.com/digitalbazaar/forge/issues/818 - new build 0.10.0 version is not working inside web workers
	/*eslint no-global-assign: "off"*/
	window = self;

	// This is the entry point for our worker
	s.addEventListener('message', async function (e) {
		const data = JSON.parse(arrayBufferToBinaryStringExp(e.data));


		const ttPolicy = window.trustedTypes.createPolicy('TTSecPolicy', {
			createScriptURL: (url) => {
				// Option A: Validate that the URL points to your expected SW file
				const parsed = new URL(url, window.location.origin);
				if (parsed.origin === window.location.origin &&
					(
						parsed.pathname === '/shared.js' || parsed.pathname === '/shared.min.js'
					)
				) {
					return url;
				}
				//else...

				// console.log("Trusted Types Violation: Unauthorized URL");
				return null;
			}
		});


		// In web workers we can use importScripts to load external javascripts
		for (const unsafeUrl of data.libs2Load) {
			// console.log(`loading script: ${unsafeUrl}...`);
			const trustedUrl = ttPolicy.createScriptURL(unsafeUrl);
			if (trustedUrl?.toString() !== "") {
				// console.log(`...trusted script URL: ${trustedUrl} loaded`);
				importScripts(trustedUrl);
			}
		}

		// Save a timestamp when we started
		let lastUpdateMilis = new Date().getTime(), currentMilis, pass, hexHash;

		let products = new LazyProductExp(data.passCharacterLength, data.alphabet);

		// Perform the loop on the range and start generating hashes :-)
		for (let i = data.range.low; i < data.range.high; i++) {
			// Hash our number password with a salt and key stretching
			pass = products.item(i);
			hexHash = await hashExp(pass);

			// Get current time for resporting the status
			currentMilis = new Date().getTime();

			// If last status update is older than updateRate in ms or fallback to 25ms
			if ((currentMilis - lastUpdateMilis) > data.updateRate) {
				// Update status to host
				const cmd = {
					update: {
						hash: hexHash,
						passphrase: pass,
						remaining: data.range.high - i
					}
				};
				const buff = binaryStringToArrayBufferExp(JSON.stringify(cmd));
				s.postMessage(buff, [buff]);
				// Update lastUpdateTime
				lastUpdateMilis = currentMilis;
			}

			// We found the hash!!!! :-)
			if (hexHash === data.hash) {
				let cmd = {
					found: {
						hash: hexHash,
						passphrase: pass,
						remaining: data.range.high - i
						//timeSpent: new Date().getTime() - startTime.getTime()
					}
				};
				const buff = binaryStringToArrayBufferExp(JSON.stringify(cmd));
				s.postMessage(buff, [buff]);
				break;
			}
		}

		const cmd = {
			done: {
				//timeSpent: new Date().getTime() - startTime.getTime(),
				hash: hexHash,
				passphrase: pass
			}
		};
		const buff = binaryStringToArrayBufferExp(JSON.stringify(cmd));
		s.postMessage(buff, [buff]);
	});

}(self));
