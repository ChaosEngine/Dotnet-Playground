// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DotnetPlayground.Helpers;
using DotnetPlayground.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentitySample.DefaultUI
{
	public abstract class ChangePasswordModelBase : PageModel
	{
		public static readonly string ASPX = "~/Identity/Account/Manage/ChangePassword";

		[BindProperty]
		public InputModel Input { get; set; }

		[TempData]
		public string StatusMessage { get; set; }

        [RequiresUnreferencedCode("The property referenced by 'NewPassword' may be trimmed. Ensure it is preserved.")]
		public class InputModel
		{
			[Required]
			[DataType(DataType.Password)]
			[Display(Name = "Current password")]
			public string OldPassword { get; set; }

			[Required]
			[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
			[DataType(DataType.Password)]
			[Display(Name = "New password")]
			public string NewPassword { get; set; }

			[DataType(DataType.Password)]
			[Display(Name = "Confirm new password")]
			[Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
			public string ConfirmPassword { get; set; }
		}

		public virtual Task<IActionResult> OnGetAsync() => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();
	}

	public class ChangePasswordModel<TUser> : ChangePasswordModelBase where TUser : class
	{
		private readonly UserManager<TUser> _userManager;
		private readonly SignInManager<TUser> _signInManager;
		private readonly ILogger<ChangePasswordModel<TUser>> _logger;

		public ChangePasswordModel(
			UserManager<TUser> userManager,
			SignInManager<TUser> signInManager,
			ILogger<ChangePasswordModel<TUser>> logger)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_logger = logger;
		}

		public override async Task<IActionResult> OnGetAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			var hasPassword = await _userManager.HasPasswordAsync(user);
			if (!hasPassword)
			{
				return Redirect("~/Identity/Account/Manage/SetPassword");
			}

			return Page();
		}

		public override async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
			if (!changePasswordResult.Succeeded)
			{
				foreach (var error in changePasswordResult.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
				return Page();
			}

			await _signInManager.RefreshSignInAsync(user);
			_logger.LogInformation("User changed their password successfully.");
			StatusMessage = "Your password has been changed.";

			return Redirect(ASPX);
		}
	}

	public class ChangePasswordModel : ChangePasswordModel<ApplicationUser>
	{
		public ChangePasswordModel(
			UserManager<ApplicationUser> userManager,
			MySignInManager signInManager,
			ILogger<ChangePasswordModel<ApplicationUser>> logger) : base(userManager, signInManager, logger)
		{
		}
	}
}
