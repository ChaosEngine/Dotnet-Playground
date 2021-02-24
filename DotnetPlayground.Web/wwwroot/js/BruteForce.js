/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "BruteForce" }]*/
/*global binaryStringToArrayBufferExp arrayBufferToBinaryStringExp libs2Load*/
function BruteForce(d,/* libsToLoad,*/ workerCount, updateRate, alphabet, hashToCrack, passCharacterLength, foundAction) {

	let passphraseLimit = Math.pow(alphabet.length/*10 digits*/, passCharacterLength/*number of digits*/),
		workers = [], startTime = null;

	function onError(e) {
		updateTextContent('#pParagraph', [
			'ERROR: Line ', e.lineno, ' in ', e.filename, ': ', e.message
		].join(''));
	}

	function toArray(obj) {
		return [].map.call(obj, function (element) {
			return element;
		});
	}

	function splitNumIntoRanges(num, count) {
		const inc = Math.ceil(num / count), chunks = [];
		let seq = 1;

		for (let i = 1; i < count; i++) {
			chunks.push({
				low: seq,
				high: seq + inc
			});

			seq += inc;
		}

		chunks.push({
			low: seq,
			high: num + 1
		});

		return chunks;
	}

	function createWorkerMonitor(index) {
		const element = d.createElement('div');
		element.id = 'worker-' + index;
		element.classList.add('worker'); element.classList.add('col-md-4');

		element.innerHTML =
			'<div class="card m-2">\
				<h5 class="card-header title">WORKER ' + index + '</h5>\
				<div class="card-body">\
					<h5 class="card-title passphrase">Passphrase</h5>\
					<p class="card-text value"></p>\
					<h5 class="card-title hash">Hash</h5>\
					<p class="card-text value"></p>\
					<h5 class="card-title remaining">Combinations left</h5>\
					<p class="card-text value"></p>\
				</div>\
			</div>';

		d.querySelector('.workers').appendChild(element);
	}

	function updateTextContent(selector, content) {
		d.querySelector(selector).textContent = content;
	}

	this.run = function () {
		updateTextContent('.global-message', 'Starting ' + workerCount +
			' workers to brute force the SHA256 hash ' + hashToCrack + '.');

		// Splitting the limit number into pieces and distribute equally along the
		// workers.
		splitNumIntoRanges(passphraseLimit, workerCount).forEach(function (range, index) {
			const suffix = libs2Load.indexOf("shared.js") === -1 ? '.min' : '';
			const worker = new Worker('../js/workers/BruteForceWorker' + suffix + '.js');

			createWorkerMonitor(index);

			worker.addEventListener('message', function (e) {
				const data = JSON.parse(arrayBufferToBinaryStringExp(e.data));

				if (data.update) {
					// On a update we update the data of the specific worker
					updateTextContent('#worker-' + index + ' .passphrase + .value', data.update.passphrase);
					updateTextContent('#worker-' + index + ' .hash + .value', data.update.hash);
					updateTextContent('#worker-' + index + ' .remaining + .value', data.update.remaining);
				}
				else if (data.found) {
					// If a worker found the correct hash we will set the global message and
					// terminate all workers.
					updateTextContent('#worker-' + index + ' .passphrase + .value', data.found.passphrase);
					updateTextContent('#worker-' + index + ' .hash + .value', data.found.hash);
					updateTextContent('#worker-' + index + ' .remaining + .value', data.found.remaining);

					d.querySelector('#worker-' + index).classList.add('found');
					updateTextContent('.global-message', 'Worker ' + index +
						' found the passphrase ' + data.found.passphrase + ' within ' +
						(new Date().getTime() - startTime.getTime()) + 'ms!');

					// Terminate all workers
					workers.forEach(function (w) {
						// Worker.terminate() to interrupt the web worker
						if (w !== null) {
							w.terminate(); w = null;
						}
					});
					workers = [];
					// Add done class to all worker elements						
					toArray(d.querySelectorAll('.worker.done:not(.found)')).forEach(function (e) {
						e.classList.add('done');
					});

					if (foundAction !== null && typeof (foundAction) !== undefined) {
						foundAction(data.found.passphrase);
					}
				}
				else if (data.done) {
					// If a worker is done before we found a result lets update the data and
					// style.
					updateTextContent('#worker-' + index + ' .passphrase + .value', data.done.passphrase);
					updateTextContent('#worker-' + index + ' .hash + .value', data.done.hash);
					updateTextContent('#worker-' + index + ' .remaining + .value', '0');
					d.querySelector('#worker-' + index).classList.add('done');

					workers[index].terminate();
					workers[index] = null;

					let all_nulls = true;
					workers.forEach(function (w) {
						if (w !== null)
							all_nulls = false;
					});
					if (all_nulls === true) {
						toArray(d.querySelectorAll('.worker.done:not(.found)')).forEach(function (e) {
							e.classList.add('failed');
						});
						updateTextContent('.global-message', 'nothing found, sorry :-/');
					}
				}
			});
			worker.addEventListener('error', onError, false);

			// Start the worker with a postMessage and pass the parameters
			const cmd = {
				libs2Load: libs2Load,
				hash: hashToCrack,
				range: range,
				alphabet: alphabet,
				passCharacterLength: passCharacterLength,
				updateRate: updateRate
			};
			const buff = binaryStringToArrayBufferExp(JSON.stringify(cmd));
			worker.postMessage(buff, [buff]);

			// Push into the global workers array so we have controll later on
			workers.push(worker);
		});

		startTime = new Date();
	};

	this.clear = function () {
		startTime = null;

		workers.forEach(function (w) {
			if (w !== null) {
				// Worker.terminate() to interrupt the web worker
				w.terminate();
			}
		});

		workers = [];

		toArray(d.querySelectorAll('.workers *')).forEach(function (node) {
			node.parentNode.removeChild(node);
		});

		updateTextContent('.global-message', '');
	};
}
