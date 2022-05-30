// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DotnetPlayground.Helpers;
using DotnetPlayground.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentitySample.DefaultUI
{
	[AllowAnonymous]
	public abstract class LoginModelBase : PageModel
	{
		[BindProperty]
		public InputModel Input { get; set; }

		public IList<AuthenticationScheme> ExternalLogins { get; set; }

		public string ReturnUrl { get; set; }

		[TempData]
		public string ErrorMessage { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			public string Email { get; set; }

			[Required]
			[DataType(DataType.Password)]
			public string Password { get; set; }

			[Display(Name = "Remember me?")]
			public bool RememberMe { get; set; }
		}

		public virtual Task OnGetAsync(string returnUrl = null) => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostAsync(string returnUrl = null) => throw new NotImplementedException();
	}

	public class LoginModel<TUser> : LoginModelBase where TUser : class
	{
		private readonly SignInManager<TUser> _signInManager;
		private readonly ILogger<LoginModel<TUser>> _logger;

		public LoginModel(SignInManager<TUser> signInManager, ILogger<LoginModel<TUser>> logger)
		{
			_signInManager = signInManager;
			_logger = logger;
		}

		public override async Task OnGetAsync(string returnUrl = null)
		{
			if (!string.IsNullOrEmpty(ErrorMessage))
			{
				ModelState.AddModelError(string.Empty, ErrorMessage);
			}

			returnUrl = returnUrl ?? Url.Content("~/");

			// Clear the existing external cookie to ensure a clean login process
			await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			ReturnUrl = returnUrl;
		}

		public override async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			returnUrl = returnUrl ?? Url.Content("~/");

			if (ModelState.IsValid)
			{
				// This doesn't count login failures towards account lockout
				// To enable password failures to trigger account lockout, set lockoutOnFailure: true
				var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
				if (result.Succeeded)
				{
					_logger.LogInformation("User logged in.");
					returnUrl = returnUrl.StartsWith(Url.Content("~/")) ? returnUrl : Url.Content("~/" + returnUrl.Substring(1));
					return LocalRedirect(returnUrl);
				}
				if (result.RequiresTwoFactor)
				{
					return Redirect($"~/Identity/Account/LoginWith2fa?returnUrl={returnUrl}&rememberMe={Input.RememberMe}");
				}
				if (result.IsLockedOut)
				{
					_logger.LogWarning("User account locked out.");
					return Redirect("~/Identity/Account/Lockout");
				}
				else
				{
					ModelState.AddModelError(string.Empty, "Invalid login attempt.");
					ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
					var ppp = Page();
					ppp.StatusCode = (int)HttpStatusCode.Unauthorized;
					return ppp;
				}
			}

			// If we got this far, something failed, redisplay form
			return Page();
		}
	}

	public class LoginModel : LoginModel<ApplicationUser>
	{
		public LoginModel(MySignInManager signInManager, ILogger<LoginModel<ApplicationUser>> logger) : base(signInManager, logger)
		{
		}
	}
}
