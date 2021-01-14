// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DotnetPlayground.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentitySample.DefaultUI
{
	[AllowAnonymous]
	public abstract class ResetPasswordModelBase : PageModel
	{
		[BindProperty]
		public InputModel Input { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			public string Email { get; set; }

			[Required]
			[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
			[DataType(DataType.Password)]
			public string Password { get; set; }

			[DataType(DataType.Password)]
			[Display(Name = "Confirm password")]
			[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
			public string ConfirmPassword { get; set; }

			[Required]
			public string Code { get; set; }

		}
		public virtual IActionResult OnGet(string code = null) => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();
	}

	public class ResetPasswordModel<TUser> : ResetPasswordModelBase where TUser : class
	{
		private readonly UserManager<TUser> _userManager;

		public ResetPasswordModel(UserManager<TUser> userManager)
		{
			_userManager = userManager;
		}

		public override IActionResult OnGet(string code = null)
		{
			if (code == null)
			{
				return BadRequest("A code must be supplied for password reset.");
			}
			else
			{
				Input = new InputModel
				{
					Code = code
				};
				return Page();
			}
		}

		public override async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var user = await _userManager.FindByEmailAsync(Input.Email);
			if (user == null)
			{
				// Don't reveal that the user does not exist
				return Redirect("ResetPasswordConfirmation");
			}

			var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
			if (result.Succeeded)
			{
				return Redirect("ResetPasswordConfirmation");
			}

			foreach (var error in result.Errors)
			{
				ModelState.AddModelError(string.Empty, error.Description);
			}
			return Page();
		}
	}

	public class ResetPasswordModel : ResetPasswordModel<ApplicationUser>
	{
		public ResetPasswordModel(UserManager<ApplicationUser> userManager) : base(userManager)
		{
		}
	}
}
