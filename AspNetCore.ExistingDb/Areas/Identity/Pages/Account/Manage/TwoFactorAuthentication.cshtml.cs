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
	public abstract class TwoFactorAuthenticationModelBase : PageModel
	{
		public bool HasAuthenticator { get; set; }

		public int RecoveryCodesLeft { get; set; }

		[BindProperty]
		public bool Is2faEnabled { get; set; }

		public bool IsMachineRemembered { get; set; }

		[TempData]
		public string StatusMessage { get; set; }

		public virtual Task<IActionResult> OnGetAsync() => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();

	}

	public class TwoFactorAuthenticationModel<TUser> : TwoFactorAuthenticationModelBase where TUser : class
	{
		private readonly UserManager<TUser> _userManager;
		private readonly SignInManager<TUser> _signInManager;
		private readonly ILogger<TwoFactorAuthenticationModel<TUser>> _logger;

		public TwoFactorAuthenticationModel(
			UserManager<TUser> userManager, SignInManager<TUser> signInManager, ILogger<TwoFactorAuthenticationModel<TUser>> logger)
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

			HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null;
			Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
			IsMachineRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user);
			RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);

			return Page();
		}

		public override async Task<IActionResult> OnPostAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			await _signInManager.ForgetTwoFactorClientAsync();
			StatusMessage = "The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.";
			return RedirectToPage();
		}
	}

	public class TwoFactorAuthenticationModel : TwoFactorAuthenticationModel<ApplicationUser>
	{
		public TwoFactorAuthenticationModel(
			UserManager<ApplicationUser> userManager, MySignInManager signInManager, ILogger<TwoFactorAuthenticationModel> logger)
			: base(userManager, signInManager, logger)
		{
		}
	}
}
