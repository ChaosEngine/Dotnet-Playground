// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using DotnetPlayground.Helpers;
using DotnetPlayground.Models;
using InkBall.Module.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentitySample.DefaultUI
{
	public interface IInputModel
	{
		string Email { get; }

		string Password { get; }
	}

	[AllowAnonymous]
	public abstract class RegisterModelBase<TInp> : PageModel
	{
		[BindProperty]
		public TInp Input { get; set; }

		public string ReturnUrl { get; set; }

		public virtual void OnGet(string returnUrl = null) => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostAsync(string returnUrl = null) => throw new NotImplementedException();
	}

	public class RegisterModel<TUser, TInp> : RegisterModelBase<TInp>
		where TUser : class, new()
		where TInp : IInputModel
	{
		protected readonly SignInManager<TUser> _signInManager;
		protected readonly UserManager<TUser> _userManager;
		protected readonly IUserStore<TUser> _userStore;
		protected readonly IUserEmailStore<TUser> _emailStore;
		protected readonly ILogger<LoginModel<TUser>> _logger;
		protected readonly IEmailSender _emailSender;

		public RegisterModel(
			UserManager<TUser> userManager,
			IUserStore<TUser> userStore,
			SignInManager<TUser> signInManager,
			ILogger<LoginModel<TUser>> logger,
			IEmailSender emailSender)
		{
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
			_signInManager = signInManager;
			_logger = logger;
			_emailSender = emailSender;
		}

		public override void OnGet(string returnUrl = null)
		{
			ReturnUrl = returnUrl;
		}

		private TUser CreateUser()
		{
			try
			{
				return Activator.CreateInstance<TUser>();
			}
			catch
			{
				throw new InvalidOperationException($"Can't create an instance of '{nameof(TUser)}'. " +
					$"Ensure that '{nameof(TUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
					$"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
			}
		}

		private IUserEmailStore<TUser> GetEmailStore()
		{
			if (!_userManager.SupportsUserEmail)
			{
				throw new NotSupportedException("The default UI requires a user store with email support.");
			}
			return (IUserEmailStore<TUser>)_userStore;
		}
	}

	[AllowAnonymous]
	public class RegisterModel : RegisterModel<ApplicationUser, RegisterModel.InputModel>
	{
		public RegisterModel(
			UserManager<ApplicationUser> userManager,
			IUserStore<ApplicationUser> userStore,
			MySignInManager signInManager,
			ILogger<LoginModel<ApplicationUser>> logger,
			IEmailSender emailSender)
			: base(userManager, userStore, signInManager, logger, emailSender)
		{
		}

        [RequiresUnreferencedCode("The property referenced by 'Password' may be trimmed. Ensure it is preserved.")]
		public class InputModel : IInputModel
		{
			[Required]
			[EmailAddress]
			[Display(Name = "Email")]
			public string Email { get; set; }

			[Required]
			[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
			[DataType(DataType.Password)]
			[Display(Name = "Password")]
			public string Password { get; set; }

			[DataType(DataType.Password)]
			[Display(Name = "Confirm password")]
			[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
			public string ConfirmPassword { get; set; }

			[Required]
			[DataType(DataType.Text)]
			[Display(Name = "Full name")]
			public string Name { get; set; }

			//[Required]
			//[Range(0, 199, ErrorMessage = "Age must be between 0 and 199 years")]
			//[Display(Name = "Age")]
			//public int Age { get; set; }

			[Display(Name = "Allow desktop notifications")]
			public bool DesktopNotifications { get; set; }

			[Display(Name = "Show chat notifications")]
			public bool ShowChatNotifications { get; set; }
		}

		public override async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			returnUrl = returnUrl ?? Url.Content("~/");
			returnUrl = returnUrl.StartsWith(Url.Content("~/")) ? returnUrl : Url.Content("~/" + returnUrl.Substring(1));
			if (ModelState.IsValid)
			{
				var user = new ApplicationUser
				{
					UserName = Input.Email,
					Email = Input.Email,
					Name = Input.Name,
					//Age = Input.Age,
					UserSettings = new ApplicationUserSettings
					{
						DesktopNotifications = Input.DesktopNotifications,
						ShowChatNotifications = Input.ShowChatNotifications
					}
				};

				await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
				await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
				var result = await _userManager.CreateAsync(user, Input.Password);
				if (result.Succeeded)
				{
					_logger.LogInformation("User created a new account with password.");

					var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
					//var callbackUrl = Url.Page(
					//	"/Account/ConfirmEmail",
					//	pageHandler: null,
					//	values: new { userId = user.Id, code = code },
					//	protocol: Request.Scheme);
					var callbackUrl = $"{Request.Scheme}://{Request.Host}" +
						Url.Content($"~/Identity/Account/ConfirmEmail?userId={WebUtility.UrlEncode(user.Id)}&code={WebUtility.UrlEncode(code)}");

					await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
						$"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

					await _signInManager.SignInAsync(user, isPersistent: false);
					return LocalRedirect(returnUrl);
				}
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
			}

			// If we got this far, something failed, redisplay form
			return Page();
		}
	}
}
