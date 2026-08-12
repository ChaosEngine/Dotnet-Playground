/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "BlogsOnLoad" }]*/
/*global bootstrap*/
"use strict";

/**
 * Blogs page onload event handler
 */
function BlogsOnLoad() {
	////////////functions start/////////////
	async function parseJsonOrText(response) {
		const contentType = response.headers.get("content-type") || "";
		if (contentType.includes("application/json")) {
			return response.json();
		}

		const text = await response.text();
		try {
			return JSON.parse(text);
		} catch (_error) {
			void _error;
			return text;
		}
	}

	async function sendUrlEncodedRequest(url, method, formData, antiForgeryToken) {
		const response = await fetch(url, {
			method: method,
			headers: {
				"Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
				"RequestVerificationToken": antiForgeryToken
			},
			body: formData,
			credentials: "same-origin"
		});

		if (!response.ok) {
			throw new Error(response.status + " " + response.statusText);
		}

		return parseJsonOrText(response);
	}

	function serializeForm(form) {
		const params = new URLSearchParams();
		new FormData(form).forEach(function (value, key) {
			params.append(key, value);
		});
		return params.toString();
	}

	function wireFormSubmit(form, submitHandler) {
		form.addEventListener("submit", function (event) {
			event.preventDefault();
			submitHandler(form);
		});
	}

	function CreateAccordionPostContent(blogId, collapsible, resultArr) {
		let acc = collapsible.querySelector(`#accordion_${blogId}`);
		if (!acc) {
			acc = document.createElement("div");
			acc.id = "accordion_" + blogId;
			acc.classList.add("accordion");
			acc.classList.add("my-2");
			collapsible.appendChild(acc);
		}

		resultArr.forEach(function (p) {
			const postId = p.postId, title = p.title, content = p.content;
			const heading = collapsible.querySelector(`#heading_${blogId}_${postId}`);
			if (!heading) {
				const card = document.createElement("div");
				card.classList.add("accordion-item");

				// Create accordion header
				const h2 = document.createElement("h2");
				h2.classList.add("accordion-header");
				h2.id = `heading_${blogId}_${postId}`;

				const button = document.createElement("button");
				button.classList.add("accordion-button", "collapsed");
				button.type = "button";
				button.setAttribute("data-bs-toggle", "collapse");
				button.setAttribute("data-bs-target", `#collapse_${blogId}_${postId}`);
				button.setAttribute("aria-expanded", "false");
				button.setAttribute("aria-controls", `collapse_${blogId}_${postId}`);
				button.textContent = title;
				h2.appendChild(button);

				// Create collapsible content div
				const collapseDiv = document.createElement("div");
				collapseDiv.id = `collapse_${blogId}_${postId}`;
				collapseDiv.classList.add("accordion-collapse", "collapse");
				collapseDiv.setAttribute("aria-labelledby", `heading_${blogId}_${postId}`);
				collapseDiv.setAttribute("data-bs-parent", `#accordion_${blogId}`);

				// Create accordion body
				const accordionBody = document.createElement("div");
				accordionBody.classList.add("accordion-body");

				// Create form
				const form = document.createElement("form");
				form.method = "post";
				form.setAttribute("data-id", blogId);
				form.classList.add("postForm", "row", "g-2");

				// Validation summary
				const validationDiv = document.createElement("div");
				validationDiv.classList.add("text-danger", "validation-summary-valid");
				validationDiv.setAttribute("data-valmsg-summary", "true");
				const ul = document.createElement("ul");
				const li = document.createElement("li");
				li.style.display = "none";
				ul.appendChild(li);
				validationDiv.appendChild(ul);
				form.appendChild(validationDiv);

				// Hidden PostId input
				const postIdInput = document.createElement("input");
				postIdInput.type = "hidden";
				postIdInput.name = "PostId";
				postIdInput.value = postId;
				postIdInput.setAttribute("data-val", "true");
				postIdInput.required = true;
				postIdInput.setAttribute("data-val-required", "The PostID field is required.");
				form.appendChild(postIdInput);

				// Title form group
				const titleGroup = document.createElement("div");
				titleGroup.classList.add("form-group");
				const titleLabel = document.createElement("label");
				titleLabel.setAttribute("for", `editForm1_${postId}`);
				titleLabel.textContent = "Title";
				const titleInput = document.createElement("input");
				titleInput.type = "text";
				titleInput.name = "Title";
				titleInput.classList.add("form-control");
				titleInput.value = title;
				titleInput.id = `editForm1_${postId}`;
				titleInput.placeholder = "title";
				titleInput.setAttribute("data-val", "true");
				titleInput.required = true;
				titleInput.setAttribute("data-val-required", "The Title field is required.");
				titleGroup.appendChild(titleLabel);
				titleGroup.appendChild(titleInput);
				form.appendChild(titleGroup);

				// Content form group
				const contentGroup = document.createElement("div");
				contentGroup.classList.add("form-group");
				const contentLabel = document.createElement("label");
				contentLabel.setAttribute("for", `editForm2_${postId}`);
				contentLabel.textContent = "Content";
				const contentTextarea = document.createElement("textarea");
				contentTextarea.name = "Content";
				contentTextarea.classList.add("form-control");
				contentTextarea.id = `editForm2_${postId}`;
				contentTextarea.rows = 3;
				contentTextarea.placeholder = "content";
				contentTextarea.setAttribute("data-val", "true");
				contentTextarea.required = true;
				contentTextarea.setAttribute("data-val-required", "The Content field is required.");
				contentTextarea.textContent = content;
				contentGroup.appendChild(contentLabel);
				contentGroup.appendChild(contentTextarea);
				form.appendChild(contentGroup);

				// Edit button container
				const editButtonDiv = document.createElement("div");
				editButtonDiv.classList.add("col-sm-12", "col-md-4", "col-lg-4");
				const editButton = document.createElement("input");
				editButton.name = "operation";
				editButton.type = "submit";
				editButton.value = "EditPost";
				editButton.setAttribute("formaction", `Blogs/EditPost/${blogId}/true`);
				editButton.classList.add("update-case", "form-control", "btn", "btn-secondary");
				editButtonDiv.appendChild(editButton);
				form.appendChild(editButtonDiv);

				// Delete button container
				const deleteButtonDiv = document.createElement("div");
				deleteButtonDiv.classList.add("mx-sm-0", "col-sm-12", "col-md-4", "col-lg-4");
				const deleteButton = document.createElement("input");
				deleteButton.name = "operation";
				deleteButton.type = "submit";
				deleteButton.value = "DeletePost";
				deleteButton.setAttribute("formaction", `Blogs/DeletePost/${blogId}/true`);
				deleteButton.classList.add("delete-case", "form-control", "btn", "btn-danger");
				deleteButton.setAttribute("formnovalidate", "formnovalidate");
				deleteButtonDiv.appendChild(deleteButton);
				form.appendChild(deleteButtonDiv);

				// Assemble the structure
				accordionBody.appendChild(form);
				collapseDiv.appendChild(accordionBody);
				card.appendChild(h2);
				card.appendChild(collapseDiv);
				acc.appendChild(card);

				wireFormSubmit(form, function (submittedForm) {
					if (submittedForm.classList.contains("postForm") === false)
						BlogFormSubmit(submittedForm);
					else
						PostFormSubmit(submittedForm);
				});
			}
			else {
				heading.querySelector(`#heading_${blogId}_${postId} > button`).innerText = title;
			}

			const addPostCollapseEl = document.getElementById("addPost_" + blogId);
			const postCollapseEl = document.getElementById("collapse_" + postId);
			if (addPostCollapseEl) {
				bootstrap.Collapse.getOrCreateInstance(addPostCollapseEl).hide();
			}
			if (postCollapseEl) {
				bootstrap.Collapse.getOrCreateInstance(postCollapseEl).hide();
			}
			collapsible.querySelector(`#addPost_${blogId} > form input[name='Title']`).value = '';
			collapsible.querySelector(`#addPost_${blogId} > form textarea`).value = '';
		});
	}

	/**
	 * Post submit form
	 * @param {HTMLFormElement} form html element
	 */
	function PostFormSubmit(form) {
		const blog_id = parseInt(form.dataset.id);
		const post_id = parseInt(form.querySelector("input[name='PostId']").value);
		//const operation = serialized_form.split('operation')[1].substr(1).trim();
		const operation = document.activeElement.value;
		const delete_operation = 'DeletePost';
		//remove title and content for delete, no need
		if (operation === delete_operation) {
			form.querySelector('input#editForm1_' + post_id)?.remove();
			form.querySelector('textarea#editForm2_' + post_id)?.remove();
		}
		const serialized_form = serializeForm(form);

		const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]').value;

		sendUrlEncodedRequest(`Blogs/${operation}/${blog_id}/true`, operation === delete_operation ? 'DELETE' : 'POST', serialized_form, antiForgeryToken).then(function (result) {
			if (result === "error") {
				alert("error");
				return;
			}
			else if (result === "deleted post") {
				const card = document.querySelector(`#heading_${blog_id}_${post_id}`).parentElement;
				card.parentElement.removeChild(card);
				return;
			}
			else {
				const collapsible = document.querySelector("#collapse_" + blog_id);
				CreateAccordionPostContent(blog_id, collapsible, [result]);
			}
		}).catch(function (error) {
			alert("error: " + error.message);
		});
	}

	/**
	 * Blog submit form
	 * @param {HTMLFormElement} form incoming form
	 */
	function BlogFormSubmit(form) {
		const tr = form.closest('tr');
		const id = form.dataset.id;
		const url = form.querySelector('#inp_' + id).value;
		const serialized_form = serializeForm(form);
		//const operation = serialized_form.split('operation')[1].substr(1).trim();
		const operation = document.activeElement.value;
		const delete_operation = 'Delete';

		if (operation !== delete_operation && (url === '' || url === tr.querySelector('label.displaying').textContent))
			return;

		const antiForgeryToken = form.querySelector('input[name="__RequestVerificationToken"]').value;

		sendUrlEncodedRequest(`Blogs/${operation}/${id}/true`, operation === delete_operation ? 'DELETE' : 'POST', serialized_form, antiForgeryToken).then(function (blog) {
			if (blog === "error") {
				alert("error");
				return;
			}
			else if (blog === "deleted") {
				tr.remove();
				return;
			}
			else {
				const edit = form.querySelector('.edit');
				edit.value = blog.url;
				const display = tr.querySelector('label.displaying');
				display.textContent = blog.url;
			}
		}).catch(function (error) {
			alert("error: " + error.message);
		});
	}
	////////////functions end/////////

	Array.prototype.slice.call(document.querySelectorAll("form.blogForm")).forEach(function (form) {
		const blog_id = parseInt(form.dataset["id"]);
		let el = document.createElement('div');
		el.classList.add("col-sm-12");
		el.classList.add("col-md-3");
		el.classList.add("col-lg-1");
		el.classList.add("mx-sm-1");
		// el.classList.add("mx-md-1");
		// el.classList.add("mx-lg-1");
		el.classList.add("px-0");
		el.classList.add("unloaded");
		// Create "Posts" collapse toggle anchor
		const postsLink = document.createElement('a');
		postsLink.setAttribute('role', 'button');
		postsLink.classList.add('form-control', 'btn', 'btn-outline-success');
		postsLink.setAttribute('data-bs-toggle', 'collapse');
		postsLink.href = `#collapse_${blog_id}`;
		postsLink.setAttribute('aria-expanded', 'false');
		postsLink.setAttribute('aria-controls', `collapse_${blog_id}`);
		postsLink.textContent = 'Posts';
		el.appendChild(postsLink);
		form.appendChild(el);


		el = document.createElement("div");
		el.id = "collapse_" + blog_id;
		el.classList.add("collapse", "mt-2", "unloaded");

		// "New Post" toggle anchor
		const newPostLink = document.createElement('a');
		newPostLink.setAttribute('role', 'button');
		newPostLink.classList.add('btn', 'btn-outline-primary');
		newPostLink.setAttribute('data-bs-toggle', 'collapse');
		newPostLink.href = `#addPost_${blog_id}`;
		newPostLink.setAttribute('aria-expanded', 'false');
		newPostLink.setAttribute('aria-controls', `addPost_${blog_id}`);
		newPostLink.textContent = 'New Post';
		el.appendChild(newPostLink);

		// Collapsible container for Add Post form
		const addPostContainer = document.createElement('div');
		addPostContainer.classList.add('collapse', 'mt-2', 'card-body', 'border');
		addPostContainer.id = `addPost_${blog_id}`;

		// Add Post form
		const addPostForm = document.createElement('form');
		addPostForm.method = 'post';
		addPostForm.setAttribute('action', `Blogs/AddPost/${blog_id}/false`);
		addPostForm.classList.add('postForm', 'row', 'g-2', 'm-3');
		addPostForm.setAttribute('data-id', blog_id);

		// Validation summary
		const addValDiv = document.createElement('div');
		addValDiv.classList.add('text-danger', 'validation-summary-valid');
		addValDiv.setAttribute('data-valmsg-summary', 'true');
		const addUl = document.createElement('ul');
		const addLi = document.createElement('li');
		addLi.style.display = 'none';
		addUl.appendChild(addLi);
		addValDiv.appendChild(addUl);
		addPostForm.appendChild(addValDiv);

		// Title input group
		const addTitleGroup = document.createElement('div');
		addTitleGroup.classList.add('col-12');
		const addTitleLabel = document.createElement('label');
		addTitleLabel.setAttribute('for', `addForm1_${blog_id}`);
		addTitleLabel.textContent = 'Title';
		const addTitleInput = document.createElement('input');
		addTitleInput.type = 'text';
		addTitleInput.name = 'Title';
		addTitleInput.classList.add('form-control');
		addTitleInput.id = `addForm1_${blog_id}`;
		addTitleInput.placeholder = 'title';
		addTitleInput.setAttribute('data-val', 'true');
		addTitleInput.required = true;
		addTitleInput.setAttribute('data-val-required', 'The Title field is required.');
		addTitleGroup.appendChild(addTitleLabel);
		addTitleGroup.appendChild(addTitleInput);
		addPostForm.appendChild(addTitleGroup);

		// Content textarea group
		const addContentGroup = document.createElement('div');
		addContentGroup.classList.add('col-12');
		const addContentLabel = document.createElement('label');
		addContentLabel.setAttribute('for', `addForm2_${blog_id}`);
		addContentLabel.textContent = 'Content';
		const addContentTextarea = document.createElement('textarea');
		addContentTextarea.name = 'Content';
		addContentTextarea.classList.add('form-control');
		addContentTextarea.id = `addForm2_${blog_id}`;
		addContentTextarea.rows = 3;
		addContentTextarea.placeholder = 'content';
		addContentTextarea.setAttribute('data-val', 'true');
		addContentTextarea.required = true;
		addContentTextarea.setAttribute('data-val-required', 'The Content field is required.');
		addContentGroup.appendChild(addContentLabel);
		addContentGroup.appendChild(addContentTextarea);
		addPostForm.appendChild(addContentGroup);

		// Submit button
		const addSubmitGroup = document.createElement('div');
		addSubmitGroup.classList.add('col-sm-12', 'col-md-4', 'col-lg-4');
		const addSubmit = document.createElement('input');
		addSubmit.name = 'operation';
		addSubmit.type = 'submit';
		addSubmit.value = 'AddPost';
		addSubmit.classList.add('add-case', 'form-control', 'btn', 'btn-primary');
		addSubmitGroup.appendChild(addSubmit);
		addPostForm.appendChild(addSubmitGroup);

		// Assemble and insert
		addPostContainer.appendChild(addPostForm);
		el.appendChild(addPostContainer);
		form.parentNode.insertBefore(el, form.nextSibling);

		wireFormSubmit(addPostForm, function (submittedForm) {
			PostFormSubmit(submittedForm);
		});

		document.getElementById("collapse_" + blog_id).addEventListener("show.bs.collapse", function (event) {
			const collapsible = event.currentTarget;
			if (collapsible.classList.contains("unloaded") === true) {
				collapsible.classList.remove("unloaded");
			}
			else
				return;

			// Action to execute once the collapsible area is expanded
			const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]').value;

			sendUrlEncodedRequest('Blogs/GetPosts/' + blog_id, 'POST', '', antiForgeryToken).then(function (result) {
				if (result === "error") {
					alert("error");
					return;
				}
				else if (result && result.length > 0) {
					CreateAccordionPostContent(blog_id, collapsible, result);
				}
			}).catch(function (error) {
				alert("error: " + error.message);
			});
		});

		wireFormSubmit(form, function (submittedForm) {
			BlogFormSubmit(submittedForm);
		});
	});
}
