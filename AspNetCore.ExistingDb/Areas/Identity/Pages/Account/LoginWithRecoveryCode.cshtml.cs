// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using IdentitySample.DefaultUI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentitySample.DefaultUI
{
	[AllowAnonymous]
	public abstract class LoginWithRecoveryCodeModelBase : PageModel
	{
		[BindProperty]
		public InputModel Input { get; set; }

		public string ReturnUrl { get; set; }

		public class InputModel
		{
			[BindProperty]
			[Required]
			[DataType(DataType.Text)]
			[Display(Name = "Recovery Code")]
			public string RecoveryCode { get; set; }
		}

		public virtual Task<IActionResult> OnGetAsync(string returnUrl = null) => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostAsync(string returnUrl = null) => throw new NotImplementedException();
	}

	public class LoginWithRecoveryCodeModel<TUser> : LoginWithRecoveryCodeModelBase where TUser : class
	{
		private readonly SignInManager<TUser> _signInManager;
		private readonly UserManager<TUser> _userManager;
		private readonly ILogger<LoginWithRecoveryCodeModel<TUser>> _logger;

		public LoginWithRecoveryCodeModel(
			SignInManager<TUser> signInManager,
			UserManager<TUser> userManager,
			ILogger<LoginWithRecoveryCodeModel<TUser>> logger)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_logger = logger;
		}

		public override async Task<IActionResult> OnGetAsync(string returnUrl = null)
		{
			// Ensure the user has gone through the username & password screen first
			var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
			if (user == null)
			{
				throw new InvalidOperationException($"Unable to load two-factor authentication user.");
			}

			ReturnUrl = returnUrl;

			return Page();
		}

		public override async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
			if (user == null)
			{
				throw new InvalidOperationException($"Unable to load two-factor authentication user.");
			}

			var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);

			var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

			var userId = await _userManager.GetUserIdAsync(user);

			if (result.Succeeded)
			{
				_logger.LogInformation("User with ID '{UserId}' logged in with a recovery code.", userId);
				return LocalRedirect(returnUrl ?? Url.Content("~/"));
			}
			if (result.IsLockedOut)
			{
				_logger.LogWarning("User with ID '{UserId}' account locked out.", userId);
				return Redirect("~/Identity/Account/Lockout");
			}
			else
			{
				_logger.LogWarning("Invalid recovery code entered for user with ID '{UserId}' ", userId);
				ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
				return Page();
			}
		}
	}

	public class LoginWithRecoveryCodeModel : LoginWithRecoveryCodeModel<ApplicationUser>
	{
		public LoginWithRecoveryCodeModel(
			SignInManager<ApplicationUser> signInManager,
			UserManager<ApplicationUser> userManager,
			ILogger<LoginWithRecoveryCodeModel<ApplicationUser>> logger)
			: base(signInManager, userManager, logger)
		{
		}
	}
}
