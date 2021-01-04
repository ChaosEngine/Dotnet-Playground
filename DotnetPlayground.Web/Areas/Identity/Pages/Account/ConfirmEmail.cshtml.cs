// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotnetPlayground.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentitySample.DefaultUI
{
	[AllowAnonymous]
	public abstract class ConfirmEmailModelBase : PageModel
	{
		public static readonly string ASPX = "~/Identity/Account/ConfirmEmail";

		public virtual Task<IActionResult> OnGetAsync(string userId, string code) => throw new NotImplementedException();
	}

	public class ConfirmEmailModel<TUser> : ConfirmEmailModelBase where TUser : class
	{
		private readonly UserManager<TUser> _userManager;

		public ConfirmEmailModel(UserManager<TUser> userManager)
		{
			_userManager = userManager;
		}

		public override async Task<IActionResult> OnGetAsync(string userId, string code)
		{
			if (userId == null || code == null)
			{
				var p = Page();
				return p;
			}

			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{userId}'.");
			}

			var result = await _userManager.ConfirmEmailAsync(user, code);
			if (!result.Succeeded)
			{
				throw new InvalidOperationException($"Error confirming email for user with ID '{userId}':");
			}

			var url = Url.Content(ASPX);
			return Redirect(url);
		}
	}

	public class ConfirmEmailModel : ConfirmEmailModel<ApplicationUser>
	{
		public ConfirmEmailModel(UserManager<ApplicationUser> userManager) : base(userManager)
		{
		}
	}
}
