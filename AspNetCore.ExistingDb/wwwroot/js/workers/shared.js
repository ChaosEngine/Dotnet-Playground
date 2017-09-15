function arrayBufferToBinaryString(buffer) {
	var binary = "";
	var bytes = new Uint8Array(buffer);
	var length = bytes.byteLength;
	for (var i = 0; i < length; i++)
		binary += String.fromCharCode(bytes[i]);
	return binary;
}

function binaryStringToArrayBuffer(bin) {
	var length = bin.length;
	var buf = new ArrayBuffer(length);
	var arr = new Uint8Array(buf);
	for (var i = 0; i < length; i++)
		arr[i] = bin.charCodeAt(i);
	return buf;
}

// Standard hashing function that uses SHA256
function hash(passphrase) {
	var hash = CryptoJS.SHA256(passphrase)
	return hash.toString();
}

function LazyProduct(iCharNum, strAlphabet) {
	//this.sets = [];
	//this.dm = [];
	strAlphabet = strAlphabet.split("");
	var len = strAlphabet.length;

	this.length = Math.pow(len, iCharNum);

	this.item = function (n) {
		for (var c = [], i = iCharNum; i--;) {
			var power = Math.pow(len, i);

			c[i] = strAlphabet[(n / power << 0) % len];
		}
		return c.join('');
	};
}
