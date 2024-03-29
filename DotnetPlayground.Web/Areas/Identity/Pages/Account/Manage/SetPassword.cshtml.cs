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

namespace IdentitySample.DefaultUI
{
	public abstract class SetPasswordModelBase : PageModel
	{
		[BindProperty]
		public InputModel Input { get; set; }

		[TempData]
		public string StatusMessage { get; set; }

        [RequiresUnreferencedCode("The property referenced by 'NewPassword' may be trimmed. Ensure it is preserved.")]
		public class InputModel
		{
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

	public class SetPasswordModel<TUser> : SetPasswordModelBase where TUser : class
	{
		private readonly UserManager<TUser> _userManager;
		private readonly SignInManager<TUser> _signInManager;

		public SetPasswordModel(
			UserManager<TUser> userManager,
			SignInManager<TUser> signInManager)
		{
			_userManager = userManager;
			_signInManager = signInManager;
		}

		public override async Task<IActionResult> OnGetAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			var hasPassword = await _userManager.HasPasswordAsync(user);

			if (hasPassword)
			{
				return Redirect("~/Identity/Account/Manage/ChangePassword");
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

			var addPasswordResult = await _userManager.AddPasswordAsync(user, Input.NewPassword);
			if (!addPasswordResult.Succeeded)
			{
				foreach (var error in addPasswordResult.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
				return Page();
			}

			await _signInManager.RefreshSignInAsync(user);
			StatusMessage = "Your password has been set.";

			return Redirect("~/Identity/Account/Manage/ChangePassword");
		}
	}

	public class SetPasswordModel : SetPasswordModel<ApplicationUser>
	{
		public SetPasswordModel(
            UserManager<ApplicationUser> userManager,
            MySignInManager signInManager) : base(userManager, signInManager)
		{
		}
	}
}
