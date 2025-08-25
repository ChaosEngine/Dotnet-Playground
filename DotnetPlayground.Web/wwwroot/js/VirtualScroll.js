/* eslint-disable no-console */
/*global myAlert, i18next*/

// Hook to i18n localization function ready
window.addEventListener(/* 'DOMContentLoaded' */'load', function () {

	function RunPage(localizeSelectorFunc) {
		////////////methods start/////////////
		let _startTime = null, _refreshClicked = "cached";

		/**
		 * https://stackoverflow.com/a/2641047/4429828
		 * @param {string} name of event
		 * @param {Function} fn is a handler function
		 */
		$.fn.bindFirst = function (name, fn) {
			// Bind as you normally would. Don't want to miss out on any jQuery magic
			this.on(name, fn);

			// Thanks to a comment by @@Martin, adding support for namespaced events too.
			this.each(function () {
				let handlers = $._data(this, 'events')[name.split('.')[0]];
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
			if (params.order !== virtOpts?.SortOrder) {
				if (params.order === undefined)
					delete virtOpts.SortOrder;
				else
					virtOpts.SortOrder = params.order;
				virtOpts.set = true;
			}
			if (params.sort !== virtOpts?.SortName) {
				if (params.sort === undefined)
					delete virtOpts.SortName;
				else
					virtOpts.SortName = params.sort;
				virtOpts.set = true;
			}
			if (params.offset === undefined && virtOpts?.PageNumber !== undefined) {
				params.offset = virtOpts.PageNumber;
			} else if ((params.offset / parseInt(params.limit) + 1) !== parseInt(virtOpts?.PageNumber)) {
				virtOpts.PageNumber = (params.offset / parseInt(params.limit) + 1);
				virtOpts.set = true;
			}
			if (params.search !== virtOpts?.SearchText) {
				if (params.search === undefined || params.search.length <= 0)
					delete virtOpts.SearchText;
				else
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

			$('#spStatus').attr("data-i18n", "virtScrol.loading");
			if (localizeSelectorFunc)
				localizeSelectorFunc('#spStatus');

			SaveParamsToStore(params, window.localStorage);
			if (params.sort)
				params.sort = params.sort[0].toUpperCase() + params.sort.substring(1);

			return params;
		}

		function validateFormatter(value, row, index, field) {
			return (field !== "Validate") ? '' :
				'<button class="btn btn-success btn-sm" title="Validate" value="Validate" onclick="clientValidate(this)" data-i18n="[title]virtScrol.validate;virtScrol.validate">Validate</button>';
		}

		function setBootstrapLocaleFromI18Next(lng) {

			const prefix = `virtScrol.bootstrapTable.`, i18nTFunc = i18next.t;

			$.fn.bootstrapTable.locales[`${lng}-${lng.toUpperCase()}`] = $.fn.bootstrapTable.locales[lng] = {
				formatAddLevel: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatAdvancedCloseButton: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatAdvancedSearch: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatAllRows: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatAutoRefresh: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatCancel: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatClearSearch: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatColumn: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatColumns: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatColumnsToggleAll: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatCopyRows: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatDeleteLevel: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatDetailPagination: function (totalRows) {
					return i18nTFunc(`${prefix}${arguments.callee.name}`, { totalRows });
				},
				formatDuplicateAlertDescription: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatDuplicateAlertTitle: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatExport: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatFilterControlSwitch: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatFilterControlSwitchHide: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatFilterControlSwitchShow: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatFullscreen: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatJumpTo: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatLoadingMessage: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatMultipleSort: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatNoMatches: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatOrder: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatPaginationSwitch: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatPaginationSwitchDown: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatPaginationSwitchUp: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatPrint: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatRecordsPerPage: function (previousHtml) {
					return previousHtml + i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatRefresh: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatSRPaginationNextText: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatSRPaginationPageText: function (page) {
					return i18nTFunc(`${prefix}${arguments.callee.name}`, { page });
				},
				formatSRPaginationPreText: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatSearch: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatShowingRows: function (pageFrom, pageTo, totalRows, totalNotFiltered) {
					if (totalNotFiltered !== undefined && totalNotFiltered > 0 && totalNotFiltered > totalRows) {
						return i18nTFunc(`${prefix}formatShowingRows0`, { pageFrom, pageTo, totalRows, totalNotFiltered });
					}
					return i18nTFunc(`${prefix}formatShowingRows1`, { pageFrom, pageTo, totalRows });
				},
				formatSort: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatSortBy: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatSortOrders: function () {
					return {
						asc: i18nTFunc(`${prefix}${arguments.callee.name}.asc`),
						desc: i18nTFunc(`${prefix}${arguments.callee.name}.desc`)
					};
				},
				formatThenBy: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatToggleCustomViewOff: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatToggleCustomViewOn: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatToggleOff: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				},
				formatToggleOn: function () {
					return i18nTFunc(`${prefix}${arguments.callee.name}`);
				}
			};

			$.extend($.fn.bootstrapTable.defaults, $.fn.bootstrapTable.locales[lng]);
		}
		////////////methods end/////////////



		////////////execution/////////////
		const jqTable = $('#table');
		//connecting events
		jqTable.data("query-params", processQueryParams);
		$("#table thead tr th:last").data("formatter", validateFormatter);

		LoadParamsFromStore(jqTable, window.localStorage);

		$.fn.bootstrapTable.methods.push('changeLocale');
		$.BootstrapTable = class extends $.BootstrapTable {

			changeLocale(localeId) {
				this.options.locale = localeId;
				this.initLocale();
				this.initPagination();
				this.initBody();
				this.initToolbar();
				this.initSearchText();

				//click-bind button event, but first in line and indicate some variable
				$("button[name='refresh']").bindFirst('click', function () {
					_refreshClicked = "refresh";
				});

				setBootstrapLocaleFromI18Next(localeId);
			}
		};

		setBootstrapLocaleFromI18Next(i18next.language);

		const table = jqTable.bootstrapTable();
		//hook to i18n language change event
		i18next.on("languageChanged", (lng) => {
			table.bootstrapTable('changeLocale', lng);
		});

		let lastScroll = false;

		$('#btninfo').on('click', function () {
			const tr = table.find('tr.highlight');
			const id = getIdFromRowElement(tr);

			const msg = (id === undefined || id === "") ?
				i18next.t('virtScrol.modalContNoSelection') :
				i18next.t('virtScrol.modalContKeySelected', { id });

			myAlert(msg, i18next.t('virtScrol.modalTit'));
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
				$('#spStatus').attr({
					"data-i18n": "virtScrol.tookMs",
					"data-i18n-options": JSON.stringify({ time: (new Date().getTime() - _startTime) })
				});
				if (localizeSelectorFunc) {
					localizeSelectorFunc('#spStatus');
					localizeSelectorFunc('#table');
				}

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
				$('#spStatus').attr("data-i18n", "virtScrol.error");

				if (localizeSelectorFunc)
					localizeSelectorFunc('#spStatus');
			});
	}

	////////////execution/////////////
	if (window.registerLocalizationOnReady && Array.isArray(window.registerLocalizationOnReady)) {
		window.registerLocalizationOnReady.push(i18nLocalizeFunc => {
			const localize = typeof i18nLocalizeFunc === "function" ? i18nLocalizeFunc : undefined;

			RunPage(localize);
		});
	}
});
