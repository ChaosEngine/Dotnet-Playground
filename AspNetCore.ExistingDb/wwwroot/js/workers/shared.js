/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "Exp" }]*/
/*global forge*/
function arrayBufferToBinaryStringExp(buffer) {
	var binary = "";
	var bytes = new Uint8Array(buffer);
	var length = bytes.byteLength;
	for (var i = 0; i < length; i++)
		binary += String.fromCharCode(bytes[i]);
	return binary;
}

function binaryStringToArrayBufferExp(bin) {
	var length = bin.length;
	var buf = new ArrayBuffer(length);
	var arr = new Uint8Array(buf);
	for (var i = 0; i < length; i++)
		arr[i] = bin.charCodeAt(i);
	return buf;
}

// md.update('The quick brown fox jumps over the lazy dog');
// console.log(md.digest().toHex());
// output: d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592

// Standard hashing function that uses SHA256
function hashExp(passphrase) {
	//var hash = CryptoJS.SHA256(passphrase)
	//return hash.toString();
	
	var md = forge.md.sha256.create();
	md.update(passphrase);
	return md.digest().toHex();
}

function LazyProductExp(iCharNum, strAlphabet) {
	let len = strAlphabet.length;

	this.length = Math.pow(len, iCharNum);
	let chars = [];
	
	this.item = function (n) {
		for (var i = iCharNum; i--;) {
			var power = Math.pow(len, i);

			// ~~ is double bitwise NOT making it truncate decimal to integer
			chars[iCharNum - i] = strAlphabet[~~(n / power) % len];
		}
		return chars.join('');
	};
}
