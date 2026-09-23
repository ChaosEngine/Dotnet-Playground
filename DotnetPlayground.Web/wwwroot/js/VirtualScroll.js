/* eslint-disable no-console */
/* global myAlert, i18next, clientValidateAll, clientValidate */

// Hook to i18n localization function ready
window.addEventListener('load', function () {
	function RunPage(localizeSelectorFunc) {
		let _startTime = null;
		let _refreshClicked = "cached";
		let _isLoading = false;
		let _pendingScrollArmed = false;
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

		function getOffset() {
			return (state.pageNumber - 1) * state.pageSize;
		}

		function getTotalPages() {
			if (state.totalRows <= 0 || state.pageSize <= 0) {
				return 1;
			}
			return Math.max(1, Math.ceil(state.totalRows / state.pageSize));
		}

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

		function updateSortIndicators() {
			$('.vs-sort-indicator').text('');
			if (state.sortName.length > 0 && state.sortOrder.length > 0) {
				const marker = state.sortOrder === 'asc' ? ' ▲' : ' ▼';
				$(`.vs-sort-indicator[data-col='${state.sortName}']`).text(marker);
			}
		}

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

		function showLoadingOverlay() {
			$overlay.removeClass('d-none').addClass('d-flex');
			$overlay.attr('aria-busy', 'true');
			if (localizeSelectorFunc) {
				localizeSelectorFunc('#vsLoadingMessage');
			}
		}

		function hideLoadingOverlay() {
			$overlay.removeClass('d-flex').addClass('d-none');
			$overlay.removeAttr('aria-busy');
		}

		function setLoadingStatus() {
			$status.attr('data-i18n', 'virtScrol.loading');
			$status.removeAttr('data-i18n-options');
			if (localizeSelectorFunc) {
				localizeSelectorFunc('#spStatus');
			}
		}

		function setErrorStatus() {
			$status.attr('data-i18n', 'virtScrol.error');
			$status.removeAttr('data-i18n-options');
			if (localizeSelectorFunc) {
				localizeSelectorFunc('#spStatus');
			}
		}

		function setLoadedStatus() {
			$status.attr({
				'data-i18n': 'virtScrol.tookMs',
				'data-i18n-options': JSON.stringify({ time: (new Date().getTime() - _startTime) })
			});
			if (localizeSelectorFunc) {
				localizeSelectorFunc('#spStatus');
			}
		}

		function normalizeCellValue(value) {
			if (value === null || value === undefined) {
				return '';
			}
			return String(value);
		}

		function renderRows(rows) {
			$body.empty();
			if (rows.length > 0) {
				rows.forEach(function (row) {
					const $tr = $('<tr></tr>').attr('data-key', normalizeCellValue(row.key));

					if (_selectedKey.length > 0 && _selectedKey === normalizeCellValue(row.key)) {
						$tr.addClass('highlight');
					}

					$('<td></td>').addClass('text-center').text(normalizeCellValue(row.key)).appendTo($tr);
					$('<td></td>').text(normalizeCellValue(row.hashMD5)).appendTo($tr);
					$('<td></td>').text(normalizeCellValue(row.hashSHA256)).appendTo($tr);

					const $validateButton = $('<button></button>')
						.attr({
							type: 'button',
							title: 'Validate',
							value: 'Validate',
							'data-i18n': '[title]virtScrol.validate;virtScrol.validate'
						})
						.addClass('btn btn-success btn-sm js-client-validate')
						.text('Validate');

					$('<td></td>').addClass('text-center').append($validateButton).appendTo($tr);
					$tr.appendTo($body);
				});
			}
			else {
				const $tr = $('<tr></tr>');
				$('<td></td>').attr({
					'colspan': '4',
					'data-i18n': 'virtScrol.bootstrapTable.formatNoMatches'
				})
				.addClass('text-center')
				.text(i18next.t('virtScrol.bootstrapTable.formatNoMatches'))
				.appendTo($tr);
				
				$tr.appendTo($body);
			}

			if (localizeSelectorFunc) {
				localizeSelectorFunc('#table');
			}
		}

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

				if (usedExtraParam === 'refresh') {
					$wrap.scrollTop(0);
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

		function bindEvents() {
			let searchTimer = null;

			$('#btnRefresh').on('click', function () {
				_refreshClicked = 'refresh';
				loadPage();
			});

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

			$pageSize.on('change', function () {
				const value = parseInt($(this).val());
				if (!Number.isNaN(value) && value > 0) {
					state.pageSize = value;
					state.pageNumber = 1;
					_refreshClicked = 'refresh';
					loadPage();
				}
			});

			$btnPrevPage.on('click', function () {
				if (_isLoading) {
					return;
				}
				moveToPreviousPage();
			});

			$btnNextPage.on('click', function () {
				if (_isLoading) {
					return;
				}
				moveToNextPage();
			});

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

			$('#table thead').on('click', '.vs-sort', function () {
				const sortField = $(this).attr('data-sort');
				if (sortField && allowedSortNames.includes(sortField)) {
					toggleSort(sortField);
				}
			});

			$('#btninfo').on('click', function () {
				const msg = _selectedKey.length === 0
					? i18next.t('virtScrol.modalContNoSelection')
					: i18next.t('virtScrol.modalContKeySelected', { id: _selectedKey });

				myAlert(msg, i18next.t('virtScrol.modalTit'));
			});

			$body.on('click', 'tr', function (event) {
				if ($(event.target).closest('button').length > 0) {
					return;
				}

				$body.find('tr.highlight').removeClass('highlight');
				$(this).addClass('highlight');

				_selectedKey = normalizeCellValue($(this).find('td:first').text());
			});

			$(document)
				.on('click', '.js-client-validate-all', clientValidateAll)
				.on('click', '.js-client-validate', function () {
					clientValidate(this);
				});

			$wrap.on('scroll', function () {
				if (_isLoading) {
					return;
				}

				const epsilon = 2;
				const scrollTop = $wrap.scrollTop();
				const viewportBottom = scrollTop + $wrap.innerHeight();
				const contentHeight = $wrap[0].scrollHeight;

				if (_pendingScrollArmed && viewportBottom + epsilon >= contentHeight) {
					_pendingScrollArmed = false;
					moveToNextPage();
				}
				else if (_pendingScrollArmed && scrollTop <= 0) {
					_pendingScrollArmed = false;
					moveToPreviousPage();
				}
				else {
					_pendingScrollArmed = true;
				}
			});

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