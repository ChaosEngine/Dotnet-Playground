// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentitySample.DefaultUI.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentitySample.DefaultUI
{
	public abstract class ExternalLoginsModelBase : PageModel
	{
		public IList<UserLoginInfo> CurrentLogins { get; set; }

		public IList<AuthenticationScheme> OtherLogins { get; set; }

		public bool ShowRemoveButton { get; set; }

		[TempData]
		public string StatusMessage { get; set; }

		public virtual Task<IActionResult> OnGetAsync() => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey) => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostLinkLoginAsync(string provider) => throw new NotImplementedException();

		public virtual Task<IActionResult> OnGetLinkLoginCallbackAsync() => throw new NotImplementedException();
	}

	public class ExternalLoginsModel<TUser> : ExternalLoginsModelBase where TUser : class
	{
		private readonly UserManager<TUser> _userManager;
		private readonly SignInManager<TUser> _signInManager;
		private readonly IUserStore<TUser> _userStore;

		public ExternalLoginsModel(
			UserManager<TUser> userManager,
			SignInManager<TUser> signInManager,
			IUserStore<TUser> userStore)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_userStore = userStore;
		}

		public override async Task<IActionResult> OnGetAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			CurrentLogins = await _userManager.GetLoginsAsync(user);
			OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
				.Where(auth => CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
				.ToList();

			string passwordHash = null;
			if (_userStore is IUserPasswordStore<TUser> userPasswordStore)
			{
				passwordHash = await userPasswordStore.GetPasswordHashAsync(user, HttpContext.RequestAborted);
			}

			ShowRemoveButton = passwordHash != null || CurrentLogins.Count > 1;
			return Page();
		}

		public override async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
			if (!result.Succeeded)
			{
				var userId = await _userManager.GetUserIdAsync(user);
				throw new InvalidOperationException($"Unexpected error occurred removing external login for user with ID '{userId}'.");
			}

			await _signInManager.RefreshSignInAsync(user);
			StatusMessage = "The external login was removed.";
			return Redirect("~/Identity/Account/Manage/ExternalLogins");
		}

		public override async Task<IActionResult> OnPostLinkLoginAsync(string provider)
		{
			// Clear the existing external cookie to ensure a clean login process
			await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

			// Request a redirect to the external login provider to link a login for the current user
			//var redirectUrl = Url.Page("./ExternalLogins", pageHandler: "LinkLoginCallback");
			var redirectUrl = Url.Content("~/Identity/Account/Manage/ExternalLogins?handler=LinkLoginCallback");
			var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
			return new ChallengeResult(provider, properties);
		}

		public override async Task<IActionResult> OnGetLinkLoginCallbackAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			var userId = await _userManager.GetUserIdAsync(user);
			var info = await _signInManager.GetExternalLoginInfoAsync(userId);
			if (info == null)
			{
				throw new InvalidOperationException($"Unexpected error occurred loading external login info for user with ID '{userId}'.");
			}

			var result = await _userManager.AddLoginAsync(user, info);
			if (!result.Succeeded)
			{
				throw new InvalidOperationException($"Unexpected error occurred adding external login for user with ID '{userId}'.");
			}

			// Clear the existing external cookie to ensure a clean login process
			await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

			StatusMessage = "The external login was added.";
			return Redirect("~/Identity/Account/Manage/ExternalLogins");
		}
	}

	public class ExternalLoginsModel : ExternalLoginsModel<ApplicationUser>
	{
		public ExternalLoginsModel(
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			IUserStore<ApplicationUser> userStore) : base(userManager, signInManager, userStore)
		{
		}
	}
}
