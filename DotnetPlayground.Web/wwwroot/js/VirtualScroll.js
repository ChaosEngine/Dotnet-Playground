/* eslint-disable no-console */

window.addEventListener('load', function () {
	////////////methods start/////////////
	let _startTime = null, _refreshClicked = "cached";

	/**
	 * https://stackoverflow.com/a/2641047/4429828
	 * @param {string} name of event
	 * @param {function} fn is a handler function
	 */
	$.fn.bindFirst = function (name, fn) {
		// Bind as you normally would. Don't want to miss out on any jQuery magic
		this.on(name, fn);

		// Thanks to a comment by @@Martin, adding support for namespaced events too.
		this.each(function () {
			let handlers = $._data(this, 'events')[name.split('.')[0]];
			//console.log(handlers);
			// take out the handler we just inserted from the end
			let handler = handlers.pop();
			// move it at the beginning
			handlers.splice(0, 0, handler);
		});
	};

	function getIdFromRowElement(rowEl) {
		const result = $(rowEl).find("td:first").text();
		return result;
	}

	function LoadParamsFromStore(jqTable, store) {
		const virtOpts = JSON.parse(store.getItem("VirtOpts"));
		if (virtOpts?.PageSize !== undefined) {
			let num = parseInt(virtOpts.PageSize);
			num = (isNaN(num) || num <= 0) ? 50 : num;
			jqTable.data("page-size", num);
		}
		if (virtOpts?.SortOrder !== undefined && ["asc", "desc"].includes(virtOpts.SortOrder)) {
			jqTable.data("sort-order", virtOpts.SortOrder);
		}
		if (virtOpts?.SortName !== undefined && ["key", "hashMD5", "hashSHA256"].includes(virtOpts.SortName)) {
			jqTable.data("sort-name", virtOpts.SortName);
		}
		if (virtOpts?.PageNumber !== undefined) {
			let num = parseInt(virtOpts.PageNumber);
			num = (isNaN(num) || num <= 0) ? 1 : num;
			jqTable.data("page-number", num);
		}
		if (virtOpts?.SearchText !== undefined && typeof virtOpts.SearchText === "string" && virtOpts.SearchText.length > 0) {
			jqTable.data("search-text", virtOpts.SearchText);
		}
	}

	function SaveParamsToStore(params, store) {
		const virtOpts = JSON.parse(store.getItem("VirtOpts")) || {};
		if (params.limit === undefined && virtOpts?.PageSize !== undefined) {
			params.limit = virtOpts.PageSize;
		} else if (params.limit !== virtOpts?.PageSize && params.limit !== "50") {
			virtOpts.PageSize = params.limit;
			virtOpts.set = true;
		}
		if (params.order === undefined && ["asc", "desc"].includes(virtOpts?.SortOrder)) {
			params.order = virtOpts.SortOrder;
		} else if (params.order !== undefined && params.order !== virtOpts?.SortOrder) {
			virtOpts.SortOrder = params.order;
			virtOpts.set = true;
		}
		if (params.sort === undefined && ["key", "hashMD5", "hashSHA256"].includes(virtOpts?.SortName)) {
			params.sort = virtOpts.SortName;
		} else if (params.sort !== undefined && params.sort !== virtOpts?.SortName) {
			virtOpts.SortName = params.sort;
			virtOpts.set = true;
		}
		if (params.offset === undefined && virtOpts?.PageNumber !== undefined) {
			params.offset = virtOpts.PageNumber;
		} else if ((params.offset / parseInt(params.limit) + 1) !== parseInt(virtOpts?.PageNumber)) {
			virtOpts.PageNumber = (params.offset / parseInt(params.limit) + 1);
			virtOpts.set = true;
		}
		if (params.search === undefined && virtOpts?.SearchText !== undefined) {
			params.search = virtOpts.SearchText;
		} else if (params.search !== undefined && params.search !== virtOpts?.SearchText && params.search.length > 0) {
			virtOpts.SearchText = params.search;
			virtOpts.set = true;
		}
		if (virtOpts.set === true) {
			delete virtOpts.set;
			store.setItem("VirtOpts", JSON.stringify(virtOpts));
		}
	}

	/**
	 * queryParams: When requesting remote data, you can send additional parameters by modifying queryParams.
	 * If queryParamsType = 'limit', the params object contains: limit, offset, search, sort, order.
	 * Else, it contains: pageSize, pageNumber, searchText, sortName, sortOrder.
	 * Return false to stop request. 
	 * @param {object} params are the query params to modify or add
	 * @returns {object} query params params
	 */
	function processQueryParams(params) {
		_startTime = new Date().getTime();
		params.ExtraParam = _refreshClicked;
		_refreshClicked = "cached";
		$('#spTimeToLoad').text('Loading...');

		SaveParamsToStore(params, window.localStorage);
		if (params.sort)
			params.sort = params.sort[0].toUpperCase() + params.sort.substring(1);

		return params;
	}

	function validateFormatter(value, row) {
		return (typeof row.hashMD5 !== "string" || typeof row.hashSHA256 !== "string") ? '' :
			'<button class="btn btn-success btn-sm" title="Validate" value="Validate" onclick="clientValidate(this)">Validate</button>';
	}
	////////////methods end/////////////



	////////////execution/////////////
	const jqTable = $('#table');
	//connecting events
	jqTable.data("query-params", processQueryParams);
	$("#table thead tr th:last").data("formatter", validateFormatter);

	LoadParamsFromStore(jqTable, window.localStorage);

	//overcomming https://github.com/wenzhixin/bootstrap-table/issues/5997 bug
	$.fn.bootstrapTable.defaults.onVirtualScroll = function () {
		return false;
	};
	const table = jqTable.bootstrapTable();
	let lastScroll = false;

	$('#exampleModal').on('show.bs.modal', function () {
		const tr = table.find('tr.highlight');
		const id = getIdFromRowElement(tr);
		const msg = (id === undefined) ? 'No row selected' : 'Key selected: ' + id;

		const modal = $(this);
		modal.find('.modal-body').text(msg);
	});

	//click-bind button event, but first in line and indicate some variable
	$("button[name='refresh']").bindFirst('click', function () {
		_refreshClicked = "refresh";
	});
	table.on('refresh.bs.table', function (params) {
		params.ExtraParam = "refresh";
	})
		// register row-click event
		.on('click-row.bs.table', function (element, row, tr) {
			tr.addClass('highlight').siblings().removeClass('highlight');
		})
		// load success
		.on('load-success.bs.table', function () {
			$('#spTimeToLoad').text('Took ' + ((new Date().getTime() - _startTime) + 'ms!'));

			lastScroll = false;
			setTimeout(function () {
				//console.log('event bind');
				const epsilon = 2;
				$('.fixed-table-body').scrollTop(epsilon).on('scroll', function () {
					//console.log('scroll = ' + $('.fixed-table-body').scrollTop());

					if (lastScroll &&
						($(this).scrollTop() + $(this).innerHeight() + epsilon) >= $(this)[0].scrollHeight) {
						console.log('end reached -> nextPage');
						lastScroll = true;

						const options = table.bootstrapTable('getOptions');
						if (options.pageNumber >= options.totalPages)
							table.bootstrapTable('selectPage', 1);
						else
							table.bootstrapTable('nextPage');
						$(this).off('scroll');
					}
					else if (lastScroll && $(this).scrollTop() <= 0) {
						console.log('top reached <- prevPage');
						lastScroll = true;

						const options = table.bootstrapTable('getOptions');
						if (options.pageNumber <= 1)
							table.bootstrapTable('selectPage', options.totalPages);
						else
							table.bootstrapTable('prevPage');
						$(this).off('scroll');
					}
					else {
						lastScroll = true;
					}
				});
			}, 0);
		})
		// load error
		.on('load-error.bs.table', function () {
			$('#spTimeToLoad').text('error!');
		});
});