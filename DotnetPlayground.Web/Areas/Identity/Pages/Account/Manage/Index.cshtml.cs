// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DotnetPlayground.Helpers;
using DotnetPlayground.Models;
using InkBall.Module.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentitySample.DefaultUI
{
	public class IndexModel : PageModel
	{
		public static readonly string ASPX = "~/Identity/Account/Manage/Index";

		private readonly UserManager<ApplicationUser> _userManager;
		private readonly MySignInManager _signInManager;
		private readonly IEmailSender _emailSender;

		public IndexModel(
			UserManager<ApplicationUser> userManager,
			MySignInManager signInManager,
			IEmailSender emailSender)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_emailSender = emailSender;
		}

		public string Username { get; set; }

		public bool IsEmailConfirmed { get; set; }

		[TempData]
		public string StatusMessage { get; set; }

		[BindProperty]
		public InputModel Input { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			public string Email { get; set; }

			[Phone]
			[Display(Name = "Phone number")]
			public string PhoneNumber { get; set; }

			[Required]
			[DataType(DataType.Text)]
			[Display(Name = "Full Name")]
			public string Name { get; set; }

			//[Required]
			//[Range(0, 199, ErrorMessage = "Age must be between 0 and 199")]
			//[Display(Name = "Age")]
			//public int Age { get; set; }

			[Display(Name = "Allow desktop notifications")]
			public bool DesktopNotifications { get; set; }

			[Display(Name = "Show chat notifications")]
			public bool ShowChatNotifications { get; set; }
		}

		public async Task<IActionResult> OnGetAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			Username = user.UserName;
			Input = new InputModel
			{
				Name = user.Name,
				//Age = user.Age,
				Email = user.Email,
				PhoneNumber = user.PhoneNumber,
				DesktopNotifications = (user.UserSettings?.DesktopNotifications).GetValueOrDefault(false),
				ShowChatNotifications = (user.UserSettings?.ShowChatNotifications).GetValueOrDefault(false)
			};

			IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
				return Redirect(ASPX);

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

			if (Input.Name != user.Name)
				user.Name = Input.Name;

			//if (Input.Age != user.Age)
			//	user.Age = Input.Age;

			if (user.UserSettings == null)
				user.UserSettings = new ApplicationUserSettings();

			if (
				Input.DesktopNotifications != (user.UserSettings?.DesktopNotifications).GetValueOrDefault(false) || 
				Input.ShowChatNotifications != (user.UserSettings?.ShowChatNotifications).GetValueOrDefault(false)
				)
			{
				user.UserSettings = new ApplicationUserSettings
				{ 
					DesktopNotifications = Input.DesktopNotifications,
					ShowChatNotifications = Input.ShowChatNotifications
				};
			}

			var updateProfileResult = await _userManager.UpdateAsync(user);
			if (!updateProfileResult.Succeeded)
				throw new InvalidOperationException($"Unexpected error ocurred updating the profile for user with ID '{user.Id}'");

			if (Input.Email != user.Email)
			{
				var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
				if (!setEmailResult.Succeeded)
					throw new InvalidOperationException($"Unexpected error occurred setting email for user with ID '{user.Id}'.");
			}

			if (Input.PhoneNumber != user.PhoneNumber)
			{
				var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
				if (!setPhoneResult.Succeeded)
					throw new InvalidOperationException($"Unexpected error occurred setting phone number for user with ID '{user.Id}'.");
			}

			await _signInManager.RefreshSignInAsync(user);
			StatusMessage = "Your profile has been updated";
			return Redirect(ASPX);
		}

		public async Task<IActionResult> OnPostSendVerificationEmailAsync()
		{
			if (!ModelState.IsValid)
				return Redirect(ASPX);

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

			var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
			//var callbackUrl = Url.Page(
			//    "/Account/ConfirmEmail",
			//    pageHandler: null,
			//    values: new { user.Id, code },
			//    protocol: Request.Scheme);
			var callbackUrl = $"{Request.Scheme}://{Request.Host}" +
				Url.Content($"~/Identity/Account/ConfirmEmail?userId={WebUtility.UrlEncode(user.Id)}&code={WebUtility.UrlEncode(code)}");

			await _emailSender.SendEmailAsync(
				user.Email,
				"Confirm your email",
				$"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

			StatusMessage = "Verification email sent. Please check your email.";
			return Redirect(ASPX);
		}
	}
}
