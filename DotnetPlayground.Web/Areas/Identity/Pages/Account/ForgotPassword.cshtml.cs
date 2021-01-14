// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DotnetPlayground.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentitySample.DefaultUI
{
	[AllowAnonymous]
	public abstract class ForgotPasswordModelBase : PageModel
	{
		public static readonly string ASPX = "~/Identity/Account/ConfirmEmail";

		[BindProperty]
		public InputModel Input { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			public string Email { get; set; }
		}

		public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();
	}

	public class ForgotPasswordModel<TUser> : ForgotPasswordModelBase where TUser : class
	{
		private readonly UserManager<TUser> _userManager;
		private readonly IEmailSender _emailSender;

		public ForgotPasswordModel(UserManager<TUser> userManager, IEmailSender emailSender)
		{
			_userManager = userManager;
			_emailSender = emailSender;
		}

		public override async Task<IActionResult> OnPostAsync()
		{
			if (ModelState.IsValid)
			{
				var user = await _userManager.FindByEmailAsync(Input.Email);
				if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
				{
					// Don't reveal that the user does not exist or is not confirmed
					return Redirect(ForgotPasswordConfirmation.ASPX);
				}

				// For more information on how to enable account confirmation and password reset please
				// visit https://go.microsoft.com/fwlink/?LinkID=532713
				var code = await _userManager.GeneratePasswordResetTokenAsync(user);
				//var callbackUrl = Url.Page(
				//	"/Account/ResetPassword",
				//	pageHandler: null,
				//	values: new { code },
				//	protocol: Request.Scheme);
				var callbackUrl = $"{Request.Scheme}://{Request.Host}" +
					Url.Content($"~/Identity/Account/ResetPassword?code={WebUtility.UrlEncode(code)}");

				await _emailSender.SendEmailAsync(
					Input.Email,
					"Reset Password",
					$"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

				return Redirect(ForgotPasswordConfirmation.ASPX);
			}

			return Page();
		}
	}

	public class ForgotPasswordModel : ForgotPasswordModel<ApplicationUser>
	{
		public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender) : base(userManager, emailSender)
		{
		}
	}
}
