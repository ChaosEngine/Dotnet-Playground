/* eslint-disable no-console */
/*global myAlert, i18next*/

window.addEventListener("load", function () {
	function RunPage(localizeSelectorFunc) {
		const table = document.getElementById("table");
		const tbody = table.querySelector("tbody");
		const spStatus = document.getElementById("spStatus");
		const spPageInfo = document.getElementById("spPageInfo");
		const btnPrevPage = document.getElementById("btnPrevPage");
		const btnNextPage = document.getElementById("btnNextPage");
		const btnRefresh = document.getElementById("btnRefresh");
		const txtSearch = document.getElementById("txtSearch");
		const selPageSize = document.getElementById("selPageSize");
		const btnInfo = document.getElementById("btninfo");

		let state = {
			pageNumber: 1,
			pageSize: 50,
			sort: "",
			order: "asc",
			search: "",
			refreshMode: "cached",
			total: 0,
			selectedKey: ""
		};

		function loadStateFromStore() {
			try {
				const virtOpts = JSON.parse(localStorage.getItem("VirtOpts") || "{}");
				if (virtOpts.PageSize) state.pageSize = Math.max(1, parseInt(virtOpts.PageSize));
				if (virtOpts.PageNumber) state.pageNumber = Math.max(1, parseInt(virtOpts.PageNumber));
				if (virtOpts.SortName) state.sort = virtOpts.SortName;
				if (virtOpts.SortOrder && ["asc", "desc"].includes(virtOpts.SortOrder)) state.order = virtOpts.SortOrder;
				if (typeof virtOpts.SearchText === "string") state.search = virtOpts.SearchText;
			} catch (_error) {
				void _error;
				// Ignore malformed localStorage data.
			}
		}

		function saveStateToStore() {
			const virtOpts = {
				PageSize: String(state.pageSize),
				PageNumber: String(state.pageNumber),
				SortName: state.sort,
				SortOrder: state.order,
				SearchText: state.search
			};
			localStorage.setItem("VirtOpts", JSON.stringify(virtOpts));
		}

		function localizeStatus(key, options) {
			spStatus.setAttribute("data-i18n", key);
			if (options) {
				spStatus.setAttribute("data-i18n-options", JSON.stringify(options));
			} else {
				spStatus.removeAttribute("data-i18n-options");
			}
			if (localizeSelectorFunc) {
				localizeSelectorFunc("#spStatus");
			}
		}

		function buildQuery(extraParam) {
			const offset = (state.pageNumber - 1) * state.pageSize;
			const params = new URLSearchParams({
				Offset: String(offset),
				Limit: String(state.pageSize),
				ExtraParam: extraParam || state.refreshMode
			});
			if (state.search) params.set("Search", state.search);
			if (state.sort) params.set("Sort", state.sort);
			if (state.order) params.set("Order", state.order);
			return params.toString();
		}

		function renderRows(rows) {
			tbody.innerHTML = "";
			(rows || []).forEach(function (row) {
				const tr = document.createElement("tr");
				tr.innerHTML = `<td>${row.key || ""}</td><td>${row.hashMD5 || ""}</td><td>${row.hashSHA256 || ""}</td><td><button class="btn btn-success btn-sm" title="Validate" value="Validate" onclick="clientValidate(this)" data-i18n="[title]virtScrol.validate;virtScrol.validate">Validate</button></td>`;
				tr.addEventListener("click", function () {
					Array.from(tbody.querySelectorAll("tr.highlight")).forEach(function (el) {
						el.classList.remove("highlight");
					});
					tr.classList.add("highlight");
					state.selectedKey = row.key || "";
				});
				tbody.appendChild(tr);
			});
			if (localizeSelectorFunc) {
				localizeSelectorFunc("#table");
			}
		}

		function renderPagination() {
			const totalPages = Math.max(1, Math.ceil(state.total / state.pageSize));
			if (state.pageNumber > totalPages) {
				state.pageNumber = totalPages;
			}
			spPageInfo.textContent = `${state.pageNumber} / ${totalPages} (${state.total})`;
			btnPrevPage.disabled = totalPages <= 1;
			btnNextPage.disabled = totalPages <= 1;
		}

		async function loadData(extraParam) {
			const started = Date.now();
			localizeStatus("virtScrol.loading");
			const query = buildQuery(extraParam);
			try {
				const response = await fetch(`Load?${query}`, {
					method: "GET",
					headers: { "Cache-Control": "no-cache" }
				});
				if (!response.ok) {
					throw new Error(response.status + " " + response.statusText);
				}
				const result = await response.json();
				state.total = result.total || 0;
				renderRows(result.rows || []);
				renderPagination();
				saveStateToStore();
				localizeStatus("virtScrol.tookMs", { time: Date.now() - started });
			} catch (error) {
				console.error(error);
				localizeStatus("virtScrol.error");
			}
			state.refreshMode = "cached";
		}

		function setupSorting() {
			Array.from(table.querySelectorAll("th[data-sort-field] button")).forEach(function (btn) {
				btn.addEventListener("click", function () {
					const sortField = btn.parentElement.getAttribute("data-sort-field");
					if (state.sort === sortField) {
						state.order = state.order === "asc" ? "desc" : "asc";
					} else {
						state.sort = sortField;
						state.order = "asc";
					}
					state.pageNumber = 1;
					loadData();
				});
			});
		}

		function setupEvents() {
			btnRefresh.addEventListener("click", function () {
				state.refreshMode = "refresh";
				loadData("refresh");
			});

			btnPrevPage.addEventListener("click", function () {
				const totalPages = Math.max(1, Math.ceil(state.total / state.pageSize));
				state.pageNumber = state.pageNumber <= 1 ? totalPages : state.pageNumber - 1;
				loadData();
			});

			btnNextPage.addEventListener("click", function () {
				const totalPages = Math.max(1, Math.ceil(state.total / state.pageSize));
				state.pageNumber = state.pageNumber >= totalPages ? 1 : state.pageNumber + 1;
				loadData();
			});

			selPageSize.addEventListener("change", function () {
				state.pageSize = parseInt(selPageSize.value) || 50;
				state.pageNumber = 1;
				loadData();
			});

			let searchTimer = null;
			txtSearch.addEventListener("input", function () {
				clearTimeout(searchTimer);
				searchTimer = setTimeout(function () {
					state.search = txtSearch.value.trim();
					state.pageNumber = 1;
					loadData();
				}, 250);
			});

			btnInfo.addEventListener("click", function () {
				const msg = state.selectedKey
					? i18next.t("virtScrol.modalContKeySelected", { id: state.selectedKey })
					: i18next.t("virtScrol.modalContNoSelection");
				myAlert(msg, i18next.t("virtScrol.modalTit"));
			});
		}

		loadStateFromStore();
		selPageSize.value = String(state.pageSize);
		txtSearch.value = state.search;
		setupSorting();
		setupEvents();
		loadData();
	}

	if (!window.localize && window.registerLocalizationOnReady && Array.isArray(window.registerLocalizationOnReady)) {
		window.registerLocalizationOnReady.push(function (localize) {
			RunPage(localize);
		});
	} else {
		RunPage(window.localize);
	}
});
