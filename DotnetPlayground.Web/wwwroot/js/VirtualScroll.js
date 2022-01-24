/* eslint-disable no-console */

window.addEventListener('load', function () {
	////////////methods start/////////////
	let _startTime = null, _refreshClicked = "cached";

	function getId(element) {
		let result = $(element).closest('tr').data('uniqueid');
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
		if (virtOpts?.SortName !== undefined && ["Key", "HashMD5", "HashSHA256"].includes(virtOpts.SortName)) {
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
		if (params.sort === undefined && ["Key", "HashMD5", "HashSHA256"].includes(virtOpts?.SortName)) {
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

		return params;
	}

	function responseHandler(response) {
		//debugger;
		const mapped = response.rows.map(function (tab) {
			return { Key: tab.key, HashMD5: tab.hashMD5, HashSHA256: tab.hashSHA256 };
		});
		response.rows = mapped;
		return response;
	}

	function validateFormatter(value, row) {
		return (typeof row.HashMD5 !== "string" || typeof row.HashSHA256 !== "string") ? ''
			: '<button class="btn btn-success btn-sm" title="Validate" value="Validate" onclick="clientValidate(this)">Validate</button>';
	}
	////////////methods end/////////////



	////////////execution/////////////
	let jqTable = $('#table');
	jqTable.data("query-params", processQueryParams);
	jqTable.data("response-handler", responseHandler);
	$("#table thead tr th:last").data("formatter", validateFormatter);

	LoadParamsFromStore(jqTable, window.localStorage);

	const table = jqTable.bootstrapTable(), epsilon = 2;
	let lastScroll = false;

	$('#exampleModal').on('show.bs.modal', function () {
		let tr = table.find('tr.highlight');
		let id = getId(tr);
		let msg = (id === undefined) ? 'No row selected' : 'Key selected: ' + id;

		let modal = $(this);
		modal.find('.modal-body').text(msg);
	});

	$("button[name='refresh']").bindFirst('click', function () {
		_refreshClicked = "refresh";
	});
	table.on('refresh.bs.table', function (params) {
		//let opts = table.bootstrapTable('getOptions');
		params.ExtraParam = "refresh";

		//localStorage.removeItem('VirtOpts');
	});
	// register row-click event
	table.on('click-row.bs.table', function (element, row, tr) {
		tr.addClass('highlight').siblings().removeClass('highlight');
	});
	// load success
	table.on('load-success.bs.table', function () {
		$('#spTimeToLoad').text('Took ' + ((new Date().getTime() - _startTime) + 'ms!'));

		lastScroll = false;
		setTimeout(function () {
			//console.log('event bind');
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
	});
	// load error
	table.on('load-error.bs.table', function () {
		$('#spTimeToLoad').text('error!');
	});
});