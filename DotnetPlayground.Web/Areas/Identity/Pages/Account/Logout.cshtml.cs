// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotnetPlayground.Helpers;
using IdentitySample.DefaultUI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentitySample.DefaultUI
{
	[AllowAnonymous]
	public abstract class LogoutModelBase : PageModel
	{
		public static readonly string ASPX = "~/Identity/Account/Logout";

		public void OnGet()
		{
		}

		public virtual Task<IActionResult> OnPost(string returnUrl = null) => throw new NotImplementedException();
	}

	public class LogoutModel<TUser> : LogoutModelBase where TUser : class
	{
		private readonly SignInManager<TUser> _signInManager;
		private readonly ILogger<LogoutModel<TUser>> _logger;

		public LogoutModel(SignInManager<TUser> signInManager, ILogger<LogoutModel<TUser>> logger)
		{
			_signInManager = signInManager;
			_logger = logger;
		}

		public override async Task<IActionResult> OnPost(string returnUrl = null)
		{
			await _signInManager.SignOutAsync();
			_logger.LogInformation("User logged out.");

			//returnUrl = returnUrl ?? Url.Content("~/Home");
			if (returnUrl != null)
			{
				return LocalRedirect(returnUrl);
			}
			else
			{
				// This needs to be a redirect so that the browser performs a new
				// request and the identity for the user gets updated.
				returnUrl = Url.Content(ASPX);
				return Redirect(returnUrl);
			}
		}
	}

	public class LogoutModel : LogoutModel<ApplicationUser>
	{
		public LogoutModel(MySignInManager signInManager, ILogger<LogoutModel<ApplicationUser>> logger)
			: base(signInManager, logger)
		{
		}
	}
}
