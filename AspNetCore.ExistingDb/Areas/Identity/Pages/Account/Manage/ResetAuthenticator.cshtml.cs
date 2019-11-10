// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using AspNetCore.ExistingDb.Helpers;
using IdentitySample.DefaultUI.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentitySample.DefaultUI
{
	public abstract class ResetAuthenticatorModelBase : PageModel
	{
		[TempData]
		public string StatusMessage { get; set; }

		public virtual Task<IActionResult> OnGet() => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();
	}

	public class ResetAuthenticatorModel<TUser> : ResetAuthenticatorModelBase where TUser : class
	{
		UserManager<TUser> _userManager;
		private readonly SignInManager<TUser> _signInManager;
		ILogger<ResetAuthenticatorModel<TUser>> _logger;

		public ResetAuthenticatorModel(
			UserManager<TUser> userManager,
			SignInManager<TUser> signInManager,
			ILogger<ResetAuthenticatorModel<TUser>> logger)
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

			return Page();
		}

		public override async Task<IActionResult> OnPostAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			await _userManager.SetTwoFactorEnabledAsync(user, false);
			await _userManager.ResetAuthenticatorKeyAsync(user);
			var userId = await _userManager.GetUserIdAsync(user);
			_logger.LogInformation("User with ID '{UserId}' has reset their authentication app key.", userId);

			await _signInManager.RefreshSignInAsync(user);
			StatusMessage = "Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.";

			return Redirect("~/Identity/Account/Manage/EnableAuthenticator");
		}
	}

	public class ResetAuthenticatorModel : ResetAuthenticatorModel<ApplicationUser>
	{
		public ResetAuthenticatorModel(
            UserManager<ApplicationUser> userManager,
            MySignInManager signInManager,
            ILogger<ResetAuthenticatorModel> logger) : base(userManager, signInManager, logger)
		{
		}
	}
}
