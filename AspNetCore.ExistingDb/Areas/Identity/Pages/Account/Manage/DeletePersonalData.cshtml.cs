// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using IdentitySample.DefaultUI.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentitySample.DefaultUI
{
	public abstract class DeletePersonalDataModelBase : PageModel
	{
		[BindProperty]
		public InputModel Input { get; set; }

		public class InputModel
		{
			[Required]
			[DataType(DataType.Password)]
			public string Password { get; set; }
		}

		public bool RequirePassword { get; set; }

		public virtual Task<IActionResult> OnGet() => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();
	}

	public class DeletePersonalDataModel<TUser> : DeletePersonalDataModelBase where TUser : class
	{
		private readonly UserManager<TUser> _userManager;
		private readonly SignInManager<TUser> _signInManager;
		private readonly ILogger<DeletePersonalDataModel<TUser>> _logger;

		public DeletePersonalDataModel(
			UserManager<TUser> userManager,
			SignInManager<TUser> signInManager,
			ILogger<DeletePersonalDataModel<TUser>> logger)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_logger = logger;
		}

		public override async Task<IActionResult> OnGet()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			RequirePassword = await _userManager.HasPasswordAsync(user);
			return Page();
		}

		public override async Task<IActionResult> OnPostAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			RequirePassword = await _userManager.HasPasswordAsync(user);
			if (RequirePassword)
			{
				if (!await _userManager.CheckPasswordAsync(user, Input.Password))
				{
					ModelState.AddModelError(string.Empty, "Password not correct.");
					return Page();
				}
			}

			var result = await _userManager.DeleteAsync(user);
			var userId = await _userManager.GetUserIdAsync(user);
			if (!result.Succeeded)
			{
				throw new InvalidOperationException($"Unexpected error occurred deleteing user with ID '{userId}'.");
			}

			await _signInManager.SignOutAsync();

			_logger.LogInformation("User with ID '{UserId}' deleted themselves.", userId);

			return Redirect("~/");
		}
	}

	public class DeletePersonalDataModel : DeletePersonalDataModel<ApplicationUser>
	{
		public DeletePersonalDataModel(
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			ILogger<DeletePersonalDataModel<ApplicationUser>> logger) : base(userManager, signInManager, logger)
		{
		}
	}
}
