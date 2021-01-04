// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using DotnetPlayground.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentitySample.DefaultUI
{
	public abstract class GenerateRecoveryCodesModelBase : PageModel
	{
		[TempData]
		public string[] RecoveryCodes { get; set; }

		[TempData]
		public string StatusMessage { get; set; }

		public virtual Task<IActionResult> OnGetAsync() => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();
	}

	public class GenerateRecoveryCodesModel<TUser> : GenerateRecoveryCodesModelBase where TUser : class
	{
		private readonly UserManager<TUser> _userManager;
		private readonly ILogger<GenerateRecoveryCodesModel<TUser>> _logger;

		public GenerateRecoveryCodesModel(
			UserManager<TUser> userManager,
			ILogger<GenerateRecoveryCodesModel<TUser>> logger)
		{
			_userManager = userManager;
			_logger = logger;
		}

		public override async Task<IActionResult> OnGetAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
			if (!isTwoFactorEnabled)
			{
				var userId = await _userManager.GetUserIdAsync(user);
				throw new InvalidOperationException($"Cannot generate recovery codes for user with ID '{userId}' because they do not have 2FA enabled.");
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

			var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
			var userId = await _userManager.GetUserIdAsync(user);
			if (!isTwoFactorEnabled)
			{
				throw new InvalidOperationException($"Cannot generate recovery codes for user with ID '{userId}' as they do not have 2FA enabled.");
			}

			var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
			RecoveryCodes = recoveryCodes.ToArray();

			_logger.LogInformation("User with ID '{UserId}' has generated new 2FA recovery codes.", userId);
			StatusMessage = "You have generated new recovery codes.";
			return Redirect("~/Identity/Account/Manage/ShowRecoveryCodes");
		}
	}

	public class GenerateRecoveryCodesModel : GenerateRecoveryCodesModel<ApplicationUser>
	{
		public GenerateRecoveryCodesModel(
			UserManager<ApplicationUser> userManager,
			ILogger<GenerateRecoveryCodesModel<ApplicationUser>> logger)
			: base(userManager, logger)
		{
		}
	}
}
