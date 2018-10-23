// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using AspNetCore.ExistingDb.Helpers;
using IdentitySample.DefaultUI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentitySample.DefaultUI
{
	[AllowAnonymous]
	public abstract class LoginWith2faModelBase : PageModel
	{
		[BindProperty]
		public InputModel Input { get; set; }

		public bool RememberMe { get; set; }

		public string ReturnUrl { get; set; }

		public class InputModel
		{
			[Required]
			[StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
			[DataType(DataType.Text)]
			[Display(Name = "Authenticator code")]
			public string TwoFactorCode { get; set; }

			[Display(Name = "Remember this machine")]
			public bool RememberMachine { get; set; }
		}

		public virtual Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null) => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null) => throw new NotImplementedException();
	}

	public class LoginWith2faModel<TUser> : LoginWith2faModelBase where TUser : class
	{
		private readonly SignInManager<TUser> _signInManager;
		private readonly UserManager<TUser> _userManager;
		private readonly ILogger<LoginWith2faModel<TUser>> _logger;

		public LoginWith2faModel(
			SignInManager<TUser> signInManager,
			UserManager<TUser> userManager,
			ILogger<LoginWith2faModel<TUser>> logger)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_logger = logger;
		}

		public override async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
		{
			// Ensure the user has gone through the username & password screen first
			var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

			if (user == null)
			{
				throw new InvalidOperationException($"Unable to load two-factor authentication user.");
			}

			ReturnUrl = returnUrl;
			RememberMe = rememberMe;

			return Page();
		}

		public override async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null)
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			returnUrl = returnUrl ?? Url.Content("~/");

			var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
			if (user == null)
			{
				throw new InvalidOperationException($"Unable to load two-factor authentication user.");
			}

			var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

			var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, Input.RememberMachine);

			var userId = await _userManager.GetUserIdAsync(user);

			if (result.Succeeded)
			{
				_logger.LogInformation("User with ID '{UserId}' logged in with 2fa.", userId);
				returnUrl = returnUrl.StartsWith(Url.Content("~/")) ? returnUrl : Url.Content("~/" + returnUrl.Substring(1));
				return LocalRedirect(returnUrl);
			}
			else if (result.IsLockedOut)
			{
				_logger.LogWarning("User with ID '{UserId}' account locked out.", userId);
				return Redirect("~/Identity/Account/Lockout");
			}
			else
			{
				_logger.LogWarning("Invalid authenticator code entered for user with ID '{UserId}'.", userId);
				ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
				return Page();
			}
		}
	}

	public class LoginWith2faModel : LoginWith2faModel<ApplicationUser>
	{
		public LoginWith2faModel(
			MySignInManager signInManager,
			UserManager<ApplicationUser> userManager,
			ILogger<LoginWith2faModel<ApplicationUser>> logger)
			: base(signInManager, userManager, logger)
		{
		}
	}
}
