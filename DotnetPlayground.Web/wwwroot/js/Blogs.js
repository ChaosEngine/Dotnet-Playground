﻿/*eslint no-unused-vars: ["error", { "varsIgnorePattern": "BlogsOnLoad" }]*/
"use strict";
function BlogsOnLoad() {
	////////////functions/////////////
	function CreateAccordionPostContent(blogId, collapsible, resultArr) {
		let acc = collapsible.querySelector("#accordion_" + blogId);
		if (!acc) {
			acc = document.createElement('div');
			acc.id = "accordion_" + blogId;
			acc.classList.add("accordion");
			acc.classList.add("my-2");
			collapsible.appendChild(acc);
		}

		resultArr.forEach(function (p) {
			const heading = collapsible.querySelector("#heading_" + blogId + '_' + p.PostId);
			if (!heading) {
				const card = document.createElement('div');
				card.classList.add("card");
				const str =
					'<div class="card-header" id="heading_' + blogId + '_' + p.PostId + '">' +
					'<h2 class="mb-0 row">' +
					'<button class="btn btn-link text-left align-content-start" type="button" data-toggle="collapse" data-target="#collapse_' + blogId + '_' + p.PostId + '" aria-expanded="false" aria-controls="collapse_' + blogId + '_' + p.PostId + '">' +
					p.Title +
					'</button>' +
					'</h2>' +
					'</div>' +
					'<div id="collapse_' + blogId + '_' + p.PostId + '" class="collapse" aria-labelledby="heading_' + blogId + '_' + p.PostId + '" data-parent="#accordion_' + blogId + '">' +
					'<div class="card-body">' +
					'<form method="post" data-id="' + blogId + '" class="postForm">' +
					'<div class="text-danger validation-summary-valid" data-valmsg-summary="true">' +
					'<ul><li style="display:none"></li></ul>' +
					'</div>' +
					'<input type="hidden" name="PostId" value="' + p.PostId + '" data-val="true" required data-val-required="The PostID field is required." />' +
					'<div class="form-group">' +
					'<label for="editForm1_' + p.PostId + '">Title</label>' +
					'<input type="text" name="Title" class="form-control" value="' + p.Title + '" id="editForm1_' + p.PostId + '" placeholder="title"' +
					' data-val="true" required data-val-required="The Title field is required.">' +
					'</div>' +
					'<div class="form-group">' +
					'<label for="editForm2_' + p.PostId + '">Content</label>' +
					'<textarea name="Content" class="form-control" id="editForm2_' + p.PostId + '" rows="3" placeholder="content"' +
					' data-val="true" required data-val-required="The Content field is required.">' + p.Content + '</textarea>' +
					'</div>' +
					'<input name="operation" type="submit" value="EditPost"' +
					' formaction="Blogs/EditPost/' + blogId + '/true"' +
					' class="update-case form-control col-sm-12 col-md-4 col-lg-4 btn btn-secondary" />' +
					'<input name="operation" type="submit" value="DeletePost"' +
					' formaction="Blogs/DeletePost/' + blogId + '/true"' +
					' class="delete-case form-control mx-md-2 mx-lg-2 col-sm-12 col-md-4 col-lg-4 btn btn-danger" formnovalidate="formnovalidate" />' +
					'</form>' +
					'</div>' +
					'</div>';
				card.innerHTML = str;
				acc.appendChild(card);

				//////////Enable validators for newly created forms////////////
				$("#collapse_" + blogId + "_" + p.PostId + " > div > form").validate({
					debug: false,
					submitHandler: function (form) {
						if (form.classList.contains("postForm") === false)
							BlogFormSubmit(form);
						else
							PostFormSubmit(form);
						return false;
					}
				});
			}
			else {
				heading.querySelector("#heading_" + blogId + "_" + p.PostId + " > h2 > button").innerText = p.Title;
			}

			$("#addPost_" + blogId).collapse("hide");
			$("#collapse_" + p.PostId).collapse("hide");
			collapsible.querySelector("#addPost_" + blogId + " > form input[name='Title']").value = '';
			collapsible.querySelector("#addPost_" + blogId + " > form textarea").value = '';
		});
	}

	function PostFormSubmit(form) {
		const blog_id = $(form).data('id');
		const serialized_form = $(form).serialize();
		const post_id = $(form).find("input[name='PostId']").val();
		//const operation = serialized_form.split('operation')[1].substr(1).trim();
		const operation = document.activeElement.value;
		const delete_operation = 'DeletePost';

		const hedrs = { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value };

		$.ajax({
			method: operation === delete_operation ? 'DELETE' : 'POST',
			url: 'Blogs/' + operation + '/' + blog_id + '/true',
			dataType: 'json',
			data: serialized_form,
			headers: hedrs
		}).done(function (result) {
			if (result === "error") {
				alert("error");
				return;
			}
			else if (result === "deleted post") {
				const card = document.querySelector("#heading_" + blog_id + "_" + post_id).parentElement;
				card.parentElement.removeChild(card);
				return;
			}

			const collapsible = document.querySelector("#collapse_" + blog_id);
			CreateAccordionPostContent(blog_id, collapsible, [result]);

		}).fail(function (jqXHR, textStatus, errorThrown) {
			alert("error: " + textStatus + " " + errorThrown);
		});
	}

	function BlogFormSubmit(form) {
		let tr = $(form).parents('tr:first');
		const id = $(form).data('id');
		const url = $(form).find('#inp_' + id).val();
		const serialized_form = $(form).serialize();
		//const operation = serialized_form.split('operation')[1].substr(1).trim();
		const operation = document.activeElement.value;
		const delete_operation = 'Delete';

		if (operation !== delete_operation && (url === '' || url === tr.find('label.displaying').text()))
			return;

		const hedrs = { 'RequestVerificationToken': $(form).find('input[name="__RequestVerificationToken"]').val() };

		$.ajax({
			method: operation === delete_operation ? 'DELETE' : 'POST',
			url: 'Blogs/' + operation + '/' + id + '/true',
			dataType: 'json',
			data: serialized_form,
			headers: operation === delete_operation ? hedrs : null
		}).done(function (blog) {
			if (blog === "error") {
				alert("error");
				return;
			}
			else if (blog === "deleted") {
				$(tr).remove();
				return;
			}

			let edit = $(form).find('.edit');
			edit.val(blog.url);
			let display = tr.find('label.displaying');
			display.text(blog.url);
		}).fail(function (jqXHR, textStatus, errorThrown) {
			alert("error: " + textStatus + " " + errorThrown);
		});
	}
	////////////functions end/////////

	Array.prototype.slice.call(document.querySelectorAll("form.blogForm")).forEach(function (form) {
		const blog_id = form.dataset["id"];
		const form_group = form.querySelector('div.form-group-sm');

		let el = document.createElement('a');
		el.classList.add("form-control");
		el.classList.add("col-sm-12");
		el.classList.add("col-md-2");
		el.classList.add("col-lg-1");
		el.classList.add("mx-sm-1");
		el.classList.add("mx-md-1");
		el.classList.add("mx-lg-1");
		el.classList.add("btn");
		el.classList.add("btn-outline-success");
		el.setAttribute("data-toggle", "collapse");
		el.setAttribute("href", '#collapse_' + blog_id);
		el.setAttribute("role", "button");
		el.setAttribute("aria-expanded", "false");
		el.setAttribute("aria-controls", "collapse_" + blog_id);
		el.innerText = 'Posts';
		form_group.appendChild(el);

		el = document.createElement('div');
		el.id = "collapse_" + blog_id;
		el.classList.add("collapse");
		el.classList.add("mt-2");
		el.classList.add("unloaded");
		el.innerHTML =
			'<a class="btn btn-outline-primary" data-toggle="collapse" href="#addPost_' + blog_id + '" role="button" aria-expanded="false" aria-controls="addPost_' + blog_id + '">' +
			'New Post' +
			'</a>' +
			'<div class="collapse mt-2 card-body border" id="addPost_' + blog_id + '">' +
			'<form method="post" action="Blogs/AddPost/' + blog_id + '/false" class="postForm" data-id="' + blog_id + '">' +
			'<div class="text-danger validation-summary-valid" data-valmsg-summary="true">' +
			'<ul><li style="display:none"></li></ul>' +
			'</div>' +
			'<div class="form-group">' +
			'<label for="addForm1_' + blog_id + '">Title</label>' +
			'<input type="text" name="Title" class="form-control" id="addForm1_' + blog_id + '" placeholder="title"' +
			' data-val="true" required data-val-required="The Title field is required.">' +
			'</div>' +
			'<div class="form-group">' +
			'<label for="addForm2_' + blog_id + '">Content</label>' +
			'<textarea name="Content" class="form-control" id="addForm2_' + blog_id + '" rows="3" placeholder="content"' +
			' data-val="true" required data-val-required="The Content field is required."></textarea>' +
			'</div>' +
			'<input name="operation" type="submit" value="AddPost" class="add-case form-control col-sm-12 col-md-4 col-lg-4 btn btn-primary" />' +
			'</form>' +
			'</div>';
		form.parentNode.insertBefore(el, form.nextSibling);

		$("#collapse_" + blog_id).on("show.bs.collapse", function (event) {
			const collapsible = event.currentTarget;
			if (collapsible.classList.contains("unloaded") === true) {
				collapsible.classList.remove("unloaded");
			}
			else
				return;

			// Action to execute once the collapsible area is expanded
			const hedrs = { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value };

			$.ajax({
				method: 'POST',
				url: 'Blogs/GetPosts/' + blog_id,
				dataType: 'json',
				headers: hedrs
			}).done(function (result) {
				if (result === "error") {
					alert("error");
					return;
				}
				if (result && result.length > 0) {
					CreateAccordionPostContent(blog_id, collapsible, result);
				}

			}).fail(function (jqXHR, textStatus, errorThrown) {
				alert("error: " + textStatus + " " + errorThrown);
			});
		});
	});

	//////////Enable page wide validators////////////
	//$(form).validate({
	$.validator.setDefaults({
		debug: false,
		submitHandler: function (form) {
			if (form.classList.contains("postForm") === false)
				BlogFormSubmit(form);
			else
				PostFormSubmit(form);
			return false;
		}
	});
}
