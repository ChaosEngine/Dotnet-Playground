/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "Exp" }]*/

/**
 * Converts data buffer to string
 * @param {ArrayBuffer} buffer of data
 * @returns {string} converted string
 */
function arrayBufferToBinaryStringExp(buffer) {
	let binary = "";
	const bytes = new Uint8Array(buffer);
	const length = bytes.byteLength;
	for (let i = 0; i < length; i++)
		binary += String.fromCharCode(bytes[i]);
	return binary;
}

/**
 * Converts string to data buffer
 * @param {string} bin string representation
 * @returns {ArrayBuffer} ArrayBuffer representation
 */
function binaryStringToArrayBufferExp(bin) {
	const length = bin.length;
	const buf = new ArrayBuffer(length);
	const arr = new Uint8Array(buf);
	for (let i = 0; i < length; i++)
		arr[i] = bin.charCodeAt(i);
	return buf;
}

// md.update('The quick brown fox jumps over the lazy dog');
// console.log(md.digest().toHex());
// output: d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592

/**
 * Standard hashing function that uses SHA256
 * @param {string} passphrase to hash
 * @returns {string} hashed passphrase
 */
async function hashExp(passphrase) {
	const msgUint8 = new TextEncoder().encode(passphrase); // encode as (utf-8) Uint8Array
	const hashBuffer = await window.crypto.subtle.digest("SHA-256", msgUint8); // hash the message
	const hashHex = new Uint8Array(hashBuffer).toHex(); // Convert ArrayBuffer to hex string.
	return hashHex;
}

/**
 * LazyProductExp class
 * @param {number} iCharNum number of characters
 * @param {string} strAlphabet alphabet to use
 */
function LazyProductExp(iCharNum, strAlphabet) {
	let len = strAlphabet.length;

	this.length = Math.pow(len, iCharNum);
	let chars = [];

	this.item = function (n) {
		for (let i = iCharNum; i--;) {
			const power = Math.pow(len, i);

			// ~~ is double bitwise NOT making it truncate decimal to integer
			chars[iCharNum - i] = strAlphabet[~~(n / power) % len];
		}
		return chars.join('');
	};
}
