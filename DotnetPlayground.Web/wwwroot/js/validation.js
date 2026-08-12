"use strict";

(function () {
	function parseBoolean(value) {
		return value === true || value === "true";
	}

	function clearValidationState(form) {
		const summary = form.querySelector("[data-valmsg-summary='true'] ul");
		if (summary) {
			summary.innerHTML = "";
		}
		Array.from(form.querySelectorAll("[data-valmsg-for]")).forEach(function (span) {
			span.textContent = "";
		});
	}

	function setFieldError(field, message) {
		const span = field.form.querySelector(`[data-valmsg-for='${field.name}']`);
		if (span) {
			span.textContent = message;
		}
	}

	function addSummaryError(form, message) {
		const summary = form.querySelector("[data-valmsg-summary='true'] ul");
		if (!summary || !message) {
			return;
		}
		const li = document.createElement("li");
		li.textContent = message;
		summary.appendChild(li);
	}

	function validateField(field) {
		if (!field.name || !parseBoolean(field.dataset.val)) {
			return true;
		}

		const value = (field.value || "").trim();
		const data = field.dataset;
		let message = "";

		if (!message && data.valRequired && !value) {
			message = data.valRequired;
		}
		if (!message && data.valEmail && value) {
			const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
			if (!emailPattern.test(value)) {
				message = data.valEmail;
			}
		}
		if (!message && data.valLength && value) {
			const min = Number(data.valLengthMin || 0);
			const max = Number(data.valLengthMax || Number.MAX_SAFE_INTEGER);
			if (value.length < min || value.length > max) {
				message = data.valLength;
			}
		}
		if (!message && data.valMinlength && value) {
			const min = Number(data.valMinlengthMin || 0);
			if (value.length < min) {
				message = data.valMinlength;
			}
		}
		if (!message && data.valMaxlength && value) {
			const max = Number(data.valMaxlengthMax || Number.MAX_SAFE_INTEGER);
			if (value.length > max) {
				message = data.valMaxlength;
			}
		}
		if (!message && data.valRegex && value) {
			const pattern = data.valRegexPattern;
			if (pattern) {
				const regex = new RegExp(pattern);
				if (!regex.test(value)) {
					message = data.valRegex;
				}
			}
		}
		if (!message && data.valRange && value) {
			const min = Number(data.valRangeMin);
			const max = Number(data.valRangeMax);
			const numValue = Number(value);
			if (Number.isNaN(numValue) || numValue < min || numValue > max) {
				message = data.valRange;
			}
		}
		if (!message && data.valEqualto && value) {
			const otherSelector = (data.valEqualtoOther || "").replace(/^\*\./, "");
			const other = field.form.querySelector(`[name$='.${otherSelector}'], [name='${otherSelector}']`);
			if (other && value !== other.value) {
				message = data.valEqualto;
			}
		}

		field.setCustomValidity(message || "");
		setFieldError(field, message);
		return !message;
	}

	function validateForm(form) {
		clearValidationState(form);
		let valid = true;
		Array.from(form.elements).forEach(function (field) {
			if (!(field instanceof HTMLElement)) return;
			if (!validateField(field)) {
				valid = false;
				addSummaryError(form, field.validationMessage);
			}
		});
		return valid;
	}

	function wireForm(form) {
		if (form.dataset.vanillaValidated === "true") {
			return;
		}
		form.dataset.vanillaValidated = "true";
		form.setAttribute("novalidate", "novalidate");

		Array.from(form.elements).forEach(function (field) {
			if (!(field instanceof HTMLElement)) return;
			field.addEventListener("input", function () {
				validateField(field);
			});
			field.addEventListener("change", function () {
				validateField(field);
			});
		});

		form.addEventListener("submit", function (event) {
			if (!validateForm(form)) {
				event.preventDefault();
				event.stopPropagation();
			}
		});
	}

	function wireAllForms() {
		Array.from(document.querySelectorAll("form")).forEach(function (form) {
			if (form.querySelector("[data-val='true']")) {
				wireForm(form);
			}
		});
	}

	document.addEventListener("DOMContentLoaded", wireAllForms);
	window.vanillaValidation = {
		wireForm,
		validateForm,
		validateField
	};
})();
