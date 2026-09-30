/* eslint-disable no-console */
/* global JQuery, myAlert, i18next, clientValidateAll, clientValidate */

window.addEventListener('load', function () {
	/**
	 * Initializes the virtual grid page and wires up all related behaviors.
	 * Hook to i18n localization function ready
	 * @param {function(string): void} localizeSelectorFunc Localization callback used to update translated DOM nodes.
	 */
	function RunPage(localizeSelectorFunc) {
		let _startTime = null;
		let _refreshClicked = "cached";
		let _isLoading = false;
		let _selectedKey = "";

		const state = {
			pageSize: 50,
			pageNumber: 1,
			sortName: "",
			sortOrder: "",
			searchText: "",
			totalRows: 0,
			currentRows: []
		};

		const allowedSortNames = ["key", "hashMD5", "hashSHA256"];
		const allowedSortOrders = ["asc", "desc"];

		const $wrap = $('#virtualGridWrap');
		const $body = $('#vsBody');
		const $status = $('#spStatus');
		const $pageInfo = $('#spPageInfo');
		const $pageNumber = $('#spPageNumber');
		const $search = $('#vsSearch');
		const $pageSize = $('#vsPageSize');
		const $overlay = $('#vsLoadingOverlay');
		const $btnPrevPage = $('#btnPrevPage');
		const $btnNextPage = $('#btnNextPage');
		const $liPrevPage = $('#liPrevPage');
		const $liNextPage = $('#liNextPage');
		const $paginationList = $('#vsPaginationList');
		const $spacerTop = $('#vsSpacerTop');
		const $spacerBottom = $('#vsSpacerBottom');
		const MAX_TOTAL_TR = 750;
		const MAX_DATA_ROWS = MAX_TOTAL_TR - 2;
		const DEFAULT_ROW_HEIGHT = 36;
		const VIRTUAL_BUFFER_ROWS = 20;

		const virtualState = {
			enabled: false,
			rowHeight: DEFAULT_ROW_HEIGHT,
			poolSize: 0,
			overscan: VIRTUAL_BUFFER_ROWS,
			scrollRaf: 0,
			isScrolling: false,
			scrollEndTimer: 0,
			lastScrollTop: 0,
			startIndex: 0,
			maxStartIndex: 0,
			poolRows: []
		};

		/**
		 * Calculates the offset for the current page.
		 * @returns {number} The zero-based row offset.
		 */
		function getOffset() {
			return (state.pageNumber - 1) * state.pageSize;
		}

		/**
		 * Determines the total number of pages from the known row count.
		 * @returns {number} The total number of pages, always at least 1.
		 */
		function getTotalPages() {
			if (state.totalRows <= 0 || state.pageSize <= 0) {
				return 1;
			}
			return Math.max(1, Math.ceil(state.totalRows / state.pageSize));
		}

		/**
		 * Restores the persisted user state from local storage.
		 */
		function loadStateFromStore() {
			const virtOpts = JSON.parse(window.localStorage.getItem("VirtOpts") || "{}");

			if (virtOpts.PageSize !== undefined) {
				const parsedPageSize = parseInt(virtOpts.PageSize, 10);
				if (!Number.isNaN(parsedPageSize) && parsedPageSize > 0) {
					state.pageSize = parsedPageSize;
				}
			}

			if (virtOpts.PageNumber !== undefined) {
				const parsedPageNumber = parseInt(virtOpts.PageNumber, 10);
				if (!Number.isNaN(parsedPageNumber) && parsedPageNumber > 0) {
					state.pageNumber = parsedPageNumber;
				}
			}

			if (typeof virtOpts.SearchText === "string") {
				state.searchText = virtOpts.SearchText;
			}

			if (typeof virtOpts.SortName === "string" && allowedSortNames.includes(virtOpts.SortName)) {
				state.sortName = virtOpts.SortName;
			}

			if (typeof virtOpts.SortOrder === "string" && allowedSortOrders.includes(virtOpts.SortOrder)) {
				state.sortOrder = virtOpts.SortOrder;
			}
		}

		/**
		 * Saves the current table state to local storage.
		 */
		function saveStateToStore() {
			const virtOpts = {};

			if (state.pageSize !== 50) {
				virtOpts.PageSize = String(state.pageSize);
			}
			if (state.pageNumber > 1) {
				virtOpts.PageNumber = String(state.pageNumber);
			}
			if (state.searchText.length > 0) {
				virtOpts.SearchText = state.searchText;
			}
			if (state.sortName.length > 0) {
				virtOpts.SortName = state.sortName;
			}
			if (state.sortOrder.length > 0) {
				virtOpts.SortOrder = state.sortOrder;
			}

			window.localStorage.setItem("VirtOpts", JSON.stringify(virtOpts));
		}

		/**
		 * Refreshes the sort direction markers in the header.
		 */
		function updateSortIndicators() {
			$('.vs-sort-indicator').text('');
			if (state.sortName.length > 0 && state.sortOrder.length > 0) {
				const marker = state.sortOrder === 'asc' ? ' ▲' : ' ▼';
				$(`.vs-sort-indicator[data-col='${state.sortName}']`).text(marker);
			}
		}

		/**
		 * Creates a table cell with safe text insertion.
		 * @param {string|number|null|undefined} text The content to set on the cell.
		 * @returns {JQuery<HTMLElement>} The created table cell.
		 */
		function createCell(text) {
			return $('<td></td>').text(text);
		}

		/**
		 * Creates the validate action button for a row.
		 * @returns {JQuery<HTMLElement>} The generated button element.
		 */
		function createValidateButton() {
			return $('<button></button>')
				.attr({
					type: 'button',
					title: 'Validate',
					value: 'Validate',
					'data-i18n': '[title]virtScrol.validate;virtScrol.validate'
				})
				.addClass('btn btn-success btn-sm js-client-validate')
				.text('Validate');
		}

		/**
		 * Creates an empty data row which is later populated.
		 * @returns {JQuery<HTMLElement>} The created row element.
		 */
		function createDataRow() {
			const $tr = $('<tr></tr>').addClass('vs-data-row');
			const $keyCell = createCell('');
			const $md5Cell = createCell('');
			const $shaCell = createCell('');
			const $actionCell = $('<td></td>').addClass('text-center').append(createValidateButton());

			$tr.append($keyCell, $md5Cell, $shaCell, $actionCell);
			$tr.data('vsCells', {
				key: $keyCell,
				md5: $md5Cell,
				sha: $shaCell
			});

			return $tr;
		}

		/**
		 * Populates a row element from the server payload.
		 * @param {JQuery<HTMLElement>} $row The row element to update.
		 * @param {object} row The data object for the row.
		 * @param {number} rowIndex The index of the row within the current result set.
		 */
		function setRowData($row, row, rowIndex) {
			const key = normalizeCellValue(row.key);
			const cells = $row.data('vsCells');

			$row.attr({
				'data-key': key,
				'data-row-index': String(rowIndex)
			});
			cells.key.text(key);
			cells.md5.text(normalizeCellValue(row.hashMD5));
			cells.sha.text(normalizeCellValue(row.hashSHA256));
			$row.toggleClass('highlight', _selectedKey.length > 0 && _selectedKey === key);
		}

		/**
		 * Sets the height of a spacer node used by virtualization.
		 * @param {JQuery<HTMLElement>} $spacer The spacer element to resize.
		 * @param {number} heightPx The new height in pixels.
		 */
		function setSpacerHeight($spacer, heightPx) {
			$spacer.css('height', `${Math.max(0, Math.round(heightPx))}px`);
		}

		/**
		 * Measures the row height based on the rendered DOM.
		 * @returns {number} The measured row height in pixels.
		 */
		function measureRowHeight() {
			const $firstRow = $body.find('> tr.vs-data-row:visible').first();
			if ($firstRow.length > 0) {
				const measured = Math.max($firstRow.outerHeight(true) || 0, DEFAULT_ROW_HEIGHT);
				virtualState.rowHeight = measured;
			}
			return virtualState.rowHeight;
		}

		/**
		 * Renders the empty-state row when a query returns no matches.
		 */
		function renderNoRecordsRow() {
			$body.empty();
			setSpacerHeight($spacerTop, 0);
			setSpacerHeight($spacerBottom, 0);
			const $tr = $('<tr></tr>').addClass('no-records-found');
			createCell(i18next.t('virtScrol.bootstrapTable.formatNoMatches')).attr({
				colspan: '4',
				'data-i18n': 'virtScrol.bootstrapTable.formatNoMatches'
			}).addClass('text-center').appendTo($tr);
			$body.append($tr);
			virtualState.enabled = false;
		}

		/**
		 * Renders the table body in a non-virtualized mode.
		 * @param {Array<object>} rows The data rows to render.
		 */
		function renderStaticRows(rows) {
			$body.empty();
			setSpacerHeight($spacerTop, 0);
			setSpacerHeight($spacerBottom, 0);
			rows.forEach(function (row, index) {
				const $tr = createDataRow();
				setRowData($tr, row, index);
				$body.append($tr);
			});
			virtualState.enabled = false;
		}

		/**
		 * Initializes the virtualized row pool for large result sets.
		 * @param {Array<object>} rows The rows that will be displayed.
		 */
		function initVirtualRows(rows) {
			const viewportHeight = Math.max($wrap.innerHeight(), 1);
			const estimatedRowHeight = virtualState.rowHeight || DEFAULT_ROW_HEIGHT;
			const viewportRows = Math.max(1, Math.ceil(viewportHeight / estimatedRowHeight));
			const overscan = Math.max(VIRTUAL_BUFFER_ROWS, Math.ceil(viewportRows / 2));
			const poolSize = Math.min(MAX_DATA_ROWS, Math.max(50, viewportRows + overscan * 2));

			virtualState.enabled = true;
			virtualState.overscan = overscan;
			virtualState.poolSize = Math.min(poolSize, rows.length);
			virtualState.maxStartIndex = Math.max(rows.length - virtualState.poolSize, 0);
			virtualState.startIndex = 0;
			virtualState.poolRows = [];
			virtualState.scrollRaf = 0;
			virtualState.isScrolling = false;
			virtualState.scrollEndTimer = 0;

			$body.empty();

			for (let i = 0; i < virtualState.poolSize; i += 1) {
				const $row = createDataRow();
				virtualState.poolRows.push($row);
				$body.append($row);
			}
			setSpacerHeight($spacerTop, 0);
			setSpacerHeight($spacerBottom, 0);
		}

		/**
		 * Renders the current virtual window offset in the scrollable dataset.
		 * @param {number} startIndex The first row index to render.
		 * @param {boolean} force Whether to ignore the current start index.
		 */
		function renderVirtualWindow(startIndex, force) {
			if (!virtualState.enabled) {
				return;
			}

			const rows = state.currentRows;
			const totalRows = rows.length;
			const maxStartIndex = Math.max(totalRows - virtualState.poolSize, 0);
			const nextStart = Math.max(0, Math.min(startIndex, maxStartIndex));

			if (!force && nextStart === virtualState.startIndex) {
				return;
			}

			virtualState.startIndex = nextStart;
			virtualState.maxStartIndex = maxStartIndex;

			const endIndex = Math.min(nextStart + virtualState.poolSize, totalRows);
			const topOffset = nextStart * virtualState.rowHeight;
			const bottomOffset = Math.max(totalRows - endIndex, 0) * virtualState.rowHeight;

			setSpacerHeight($spacerTop, topOffset);
			setSpacerHeight($spacerBottom, bottomOffset);

			virtualState.poolRows.forEach(function ($row, poolIndex) {
				const rowIndex = nextStart + poolIndex;
				if (rowIndex < endIndex) {
					setRowData($row, rows[rowIndex], rowIndex);
					$row.show();
				}
				else {
					$row.hide();
				}
			});
		}

		/**
		 * Handles the virtual scroll position and triggers edge paging when needed.
		 * @param {number} scrollTop The current vertical scroll position.
		 */
		function handleVirtualScroll(scrollTop) {
			const previousScrollTop = virtualState.lastScrollTop || 0;
			const direction = scrollTop > previousScrollTop ? 1 : scrollTop < previousScrollTop ? -1 : 0;
			virtualState.lastScrollTop = scrollTop;

			if (handleEdgePaging(scrollTop, direction)) {
				return;
			}

			if (!virtualState.enabled) {
				return;
			}

			virtualState.isScrolling = true;
			if (virtualState.scrollEndTimer) {
				clearTimeout(virtualState.scrollEndTimer);
			}
			virtualState.scrollEndTimer = window.setTimeout(function () {
				virtualState.isScrolling = false;
				virtualState.scrollEndTimer = 0;
				if (virtualState.scrollRaf) {
					cancelAnimationFrame(virtualState.scrollRaf);
					virtualState.scrollRaf = 0;
				}
			}, 100);

			if (virtualState.scrollRaf) {
				cancelAnimationFrame(virtualState.scrollRaf);
			}

			virtualState.scrollRaf = window.requestAnimationFrame(function () {
				virtualState.scrollRaf = 0;
				const rawIndex = Math.floor(scrollTop / virtualState.rowHeight);
				const nextStart = Math.max(0, Math.min(rawIndex - virtualState.overscan, virtualState.maxStartIndex));
				renderVirtualWindow(nextStart, false);
			});
		}

		/**
		 * Detects when the user is near the top or bottom edge of the scroll area.
		 * @param {number} scrollTop The current scroll offset.
		 * @param {number} direction The scroll direction; -1 for up, 1 for down, 0 for neutral.
		 * @returns {boolean} True when the grid should page to the adjacent range.
		 */
		function handleEdgePaging(scrollTop, direction) {
			if (_isLoading || direction === 0) {
				return false;
			}

			const edgeThreshold = Math.max(80, virtualState.rowHeight * 2);
			const nearTop = scrollTop <= edgeThreshold;
			const nearBottom = scrollTop + $wrap.innerHeight() >= $wrap[0].scrollHeight - edgeThreshold;

			if (direction < 0 && nearTop) {
				moveToPreviousPage();
				return true;
			}

			if (direction > 0 && nearBottom) {
				moveToNextPage();
				return true;
			}

			return false;
		}

		/**
		 * Renders the active rows and enables virtualization when the result count is large.
		 * @param {Array<object>} rows The rows to display.
		 */
		function renderRows(rows) {
			if (!rows.length) {
				renderNoRecordsRow();
				return;
			}

			if (rows.length > MAX_DATA_ROWS) {
				initVirtualRows(rows);
				renderVirtualWindow(0, true);
				measureRowHeight();
				renderVirtualWindow(0, true);
			}
			else {
				renderStaticRows(rows);
			}

			if (localizeSelectorFunc) {
				localizeSelectorFunc('#table');
			}
		}

		/**
		 * Generates the numbered pagination list displayed at the footer.
		 * @param {number} totalPages The overall number of pages.
		 * @param {number} currentPage The currently selected page.
		 */
		function renderPaginationNumbers(totalPages, currentPage) {
			$paginationList.find('.vs-page-number-item').remove();

			const maxVisiblePages = 10;
			let pageStart = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
			let pageEnd = Math.min(totalPages, pageStart + maxVisiblePages - 1);

			if ((pageEnd - pageStart + 1) < maxVisiblePages) {
				pageStart = Math.max(1, pageEnd - maxVisiblePages + 1);
			}

			for (let pageIndex = pageStart; pageIndex <= pageEnd; pageIndex += 1) {
				const $li = $('<li></li>').addClass('page-item vs-page-number-item');
				const $button = $('<button></button>')
					.attr({
						type: 'button',
						'data-page': String(pageIndex)
					})
					.addClass('page-link vs-page-index')
					.text(String(pageIndex));

				if (pageIndex === currentPage) {
					$li.addClass('active');
					$button.attr('aria-current', 'page');
				}

				$li.append($button).insertBefore($liNextPage);
			}
		}

		/**
		 * Updates the page summary and pagination controls.
		 */
		function updatePaginationInfo() {
			const totalPages = getTotalPages();
			const currentPage = Math.min(Math.max(state.pageNumber, 1), totalPages);
			$pageNumber.text(i18next.t('virtScrol.bootstrapTable.formatSRPaginationPageText', { currentPage, totalPages }));
			renderPaginationNumbers(totalPages, currentPage);

			const hasMultiplePages = totalPages > 1;
			const prevDisabled = _isLoading || !hasMultiplePages/* || currentPage <= 1 */;
			const nextDisabled = _isLoading || !hasMultiplePages/* || currentPage >= totalPages */;
			$btnPrevPage.prop('disabled', prevDisabled);
			$btnNextPage.prop('disabled', nextDisabled);
			$liPrevPage.toggleClass('disabled', prevDisabled);
			$liNextPage.toggleClass('disabled', nextDisabled);

			if (state.totalRows <= 0) {
				$pageInfo.text(i18next.t('virtScrol.bootstrapTable.formatNoMatches'));
				return;
			}

			const pageFrom = getOffset() + 1;
			const pageTo = Math.min(getOffset() + state.currentRows.length, state.totalRows);
			$pageInfo.text(i18next.t('virtScrol.bootstrapTable.formatShowingRows1', {
				pageFrom,
				pageTo,
				totalRows: state.totalRows
			}));
		}

		/**
		 * Shows the loading overlay while the page request is in progress.
		 */
		function showLoadingOverlay() {
			$overlay.removeClass('d-none').addClass('d-flex');
			$overlay.attr('aria-busy', 'true');
			if (localizeSelectorFunc) {
				localizeSelectorFunc('#vsLoadingMessage');
			}
		}

		/**
		 * Hides the loading overlay after the fetch is complete.
		 */
		function hideLoadingOverlay() {
			$overlay.removeClass('d-flex').addClass('d-none');
			$overlay.removeAttr('aria-busy');
		}

		/**
		 * Sets the status text to the loading state.
		 */
		function setLoadingStatus() {
			$status.attr('data-i18n', 'virtScrol.loading');
			$status.removeAttr('data-i18n-options');
			if (localizeSelectorFunc) {
				localizeSelectorFunc('#spStatus');
			}
		}

		/**
		 * Sets the status text to the error state.
		 */
		function setErrorStatus() {
			$status.attr('data-i18n', 'virtScrol.error');
			$status.removeAttr('data-i18n-options');
			if (localizeSelectorFunc) {
				localizeSelectorFunc('#spStatus');
			}
		}

		/**
		 * Sets the status text to the loaded state with elapsed time data.
		 */
		function setLoadedStatus() {
			$status.attr({
				'data-i18n': 'virtScrol.tookMs',
				'data-i18n-options': JSON.stringify({ time: (new Date().getTime() - _startTime) })
			});
			if (localizeSelectorFunc) {
				localizeSelectorFunc('#spStatus');
			}
		}

		/**
		 * Normalizes null/undefined values to empty strings.
		 * @param {object|string|number|boolean|null|undefined} value The value to normalize.
		 * @returns {string} A string representation of the value, or an empty string.
		 */
		function normalizeCellValue(value) {
			if (value === null || value === undefined) {
				return '';
			}
			return String(value);
		}

		/**
		 * Builds the request URL for the next page fetch.
		 * @returns {string} The URL with query string parameters.
		 */
		function buildRequestUrl() {
			const params = new URLSearchParams();
			const offset = getOffset();

			params.set('Limit', String(state.pageSize));
			params.set('Offset', String(offset));
			params.set('ExtraParam', _refreshClicked);

			if (state.searchText.length > 0) {
				params.set('Search', state.searchText);
			}
			if (state.sortName.length > 0 && state.sortOrder.length > 0) {
				const serverSort = state.sortName[0].toUpperCase() + state.sortName.substring(1);
				params.set('Sort', serverSort);
				params.set('Order', state.sortOrder);
			}

			return `Load?${params.toString()}`;
		}

		/**
		 * Fetches the active page from the server and refreshes the grid.
		 * @returns {Promise<void>} Resolves when the request and render cycle complete.
		 */
		async function loadPage() {
			if (_isLoading) {
				return;
			}

			_isLoading = true;
			_startTime = new Date().getTime();
			showLoadingOverlay();
			setLoadingStatus();
			updatePaginationInfo();

			const requestUrl = buildRequestUrl();
			const usedExtraParam = _refreshClicked;
			_refreshClicked = 'cached';

			try {
				const response = await fetch(requestUrl, {
					method: 'GET',
					headers: {
						'Accept': 'application/json'
					}
				});

				if (!response.ok) {
					throw new Error(`Request failed: ${response.status}`);
				}

				const data = await response.json();
				const rows = Array.isArray(data.rows) ? data.rows : [];

				state.totalRows = Number.isFinite(data.total) ? data.total : 0;
				state.currentRows = rows;

				const totalPages = getTotalPages();
				if (state.pageNumber > totalPages) {
					state.pageNumber = totalPages;
					saveStateToStore();
					_isLoading = false;
					await loadPage();
					return;
				}

				renderRows(rows);
				updateSortIndicators();
				updatePaginationInfo();
				setLoadedStatus();
				saveStateToStore();
				$wrap.scrollTop(0);
				virtualState.lastScrollTop = 0;

				if (usedExtraParam === 'refresh') {
					if (virtualState.enabled) {
						renderVirtualWindow(0, true);
					}
				}
			}
			catch (err) {
				console.error(err);
				setErrorStatus();
			}
			finally {
				_isLoading = false;
				hideLoadingOverlay();
				updatePaginationInfo();
			}
		}

		/**
		 * Toggles the sort state of the named field.
		 * @param {string} fieldName The field to sort by.
		 */
		function toggleSort(fieldName) {
			if (state.sortName !== fieldName) {
				state.sortName = fieldName;
				state.sortOrder = 'asc';
			}
			else if (state.sortOrder === 'asc') {
				state.sortOrder = 'desc';
			}
			else if (state.sortOrder === 'desc') {
				state.sortName = '';
				state.sortOrder = '';
			}
			else {
				state.sortOrder = 'asc';
			}

			state.pageNumber = 1;
			_refreshClicked = 'refresh';
			loadPage();
		}

		/**
		 * Advances the view to the next page.
		 */
		function moveToNextPage() {
			const totalPages = getTotalPages();
			if (state.pageNumber < totalPages) {
				state.pageNumber += 1;
				loadPage();
			}
			else {
				state.pageNumber = 1;
				loadPage();
			}
		}

		/**
		 * Moves the view to the previous page.
		 */
		function moveToPreviousPage() {
			if (state.pageNumber > 1) {
				state.pageNumber -= 1;
				loadPage();
			}
			else {
				state.pageNumber = getTotalPages();
				loadPage();
			}
		}

		/**
		 * Binds all user interaction handlers to the page controls.
		 */
		function bindEvents() {
			let searchTimer = null;

			/**
			 * Refresh button click handler.
			 */
			$('#btnRefresh').on('click', function () {
				_refreshClicked = 'refresh';
				loadPage();
			});

			/**
			 * Search input handler.
			 */
			$search.on('input', function () {
				const value = $(this).val();
				state.searchText = typeof value === 'string' ? value.trim() : '';
				state.pageNumber = 1;
				_refreshClicked = 'refresh';

				if (searchTimer !== null) {
					clearTimeout(searchTimer);
				}

				searchTimer = setTimeout(function () {
					loadPage();
				}, 250);
			});

			/**
			 * Page size change handler.
			 */
			$pageSize.on('change', function () {
				const value = parseInt($(this).val());
				if (!Number.isNaN(value) && value > 0) {
					state.pageSize = value;
					state.pageNumber = 1;
					_refreshClicked = 'refresh';
					loadPage();
				}
			});

			/**
			 * Previous page button click handler.
			 */
			$btnPrevPage.on('click', function () {
				if (_isLoading) {
					return;
				}
				moveToPreviousPage();
			});

			/**
			 * Next page button click handler.
			 */
			$btnNextPage.on('click', function () {
				if (_isLoading) {
					return;
				}
				moveToNextPage();
			});

			/**
			 * Page index (number button) click handler.
			 */
			$paginationList.on('click', '.vs-page-index', function () {
				if (_isLoading) {
					return;
				}

				const selectedPage = parseInt($(this).attr('data-page'));
				if (!Number.isNaN(selectedPage) && selectedPage > 0 && selectedPage !== state.pageNumber) {
					state.pageNumber = selectedPage;
					loadPage();
				}
			});

			/**
			 * Column header sort click handler.
			 */
			$('#table thead').on('click', '.vs-sort', function () {
				const sortField = $(this).attr('data-sort');
				if (sortField && allowedSortNames.includes(sortField)) {
					toggleSort(sortField);
				}
			});

			/**
			 * Info button click handler.
			 */
			$('#btninfo').on('click', function () {
				const msg = _selectedKey.length === 0
					? i18next.t('virtScrol.modalContNoSelection')
					: i18next.t('virtScrol.modalContKeySelected', { id: _selectedKey });

				myAlert(msg, i18next.t('virtScrol.modalTit'));
			});

			/**
			 * Table row click handler.
			 */
			$body.on('click', 'tr', function (event) {
				if ($(event.target).closest('button').length > 0) {
					return;
				}

				$body.find('tr.highlight').removeClass('highlight');
				$(this).addClass('highlight');

				_selectedKey = normalizeCellValue($(this).find('td:first').text());
			});

			/**
			 * Client validation button click handlers.
			 */
			$(document)
				.on('click', '.js-client-validate-all', clientValidateAll)
				.on('click', '.js-client-validate', function () {
					clientValidate(this);
				});

			/**
			 * Virtual scroll handler.
			 */
			$wrap.on('scroll', function () {
				if (_isLoading) {
					return;
				}

				handleVirtualScroll($wrap.scrollTop());
			});

			/**
			 * Mouse wheel scroll handler.
			 */
			$wrap.on('wheel', function (event) {
				if (_isLoading) {
					return;
				}

				const wheelEvent = event.originalEvent || event;
				const deltaY = typeof wheelEvent.deltaY === 'number' ? wheelEvent.deltaY : 0;
				if (deltaY === 0) {
					return;
				}

				if (handleEdgePaging($wrap.scrollTop(), deltaY > 0 ? 1 : -1)) {
					event.preventDefault();
				}
			});

			/**
			 * Key down handler for edge paging.
			 */
			$wrap.on('keydown', function (event) {
				if (_isLoading) {
					return;
				}

				let direction;
				switch (event.key) {
					case 'ArrowUp':
					case 'PageUp':
					case 'Home':
						direction = -1;
						break;
					case 'ArrowDown':
					case 'PageDown':
					case 'End':
						direction = 1;
						break;
					default:
						return;
				}

				if (handleEdgePaging($wrap.scrollTop(), direction)) {
					event.preventDefault();
				}
			});

			/**
			 * Language change handler.
			 */
			i18next.on('languageChanged', function () {
				if (localizeSelectorFunc) {
					localizeSelectorFunc('#toolbar');
					localizeSelectorFunc('#table');
					localizeSelectorFunc('#spStatus');
					localizeSelectorFunc('#vsFooter');
					localizeSelectorFunc('#vsLoadingMessage');
				}
				updatePaginationInfo();
			});
		}

		/**
		 * Initializes the form controls from the current view state.
		 */
		function initControls() {
			$search.val(state.searchText);
			$pageSize.val(String(state.pageSize));

			if (localizeSelectorFunc) {
				localizeSelectorFunc('#toolbar');
				localizeSelectorFunc('#table');
				localizeSelectorFunc('#vsFooter');
				localizeSelectorFunc('#vsLoadingMessage');
			}
			updatePaginationInfo();
		}

		loadStateFromStore();
		initControls();
		bindEvents();
		loadPage();
	}

	if (!window.localize && window.registerLocalizationOnReady && Array.isArray(window.registerLocalizationOnReady)) {
		window.registerLocalizationOnReady.push(function (localize) {
			RunPage(localize);
		});
	}
	else {
		RunPage(window.localize);
	}
});