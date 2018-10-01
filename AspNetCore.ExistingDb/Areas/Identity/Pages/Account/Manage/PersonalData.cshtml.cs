// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using IdentitySample.DefaultUI.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentitySample.DefaultUI
{
	public abstract class PersonalDataModelBase : PageModel
	{
		public static readonly string ASPX = "~/Identity/Account/Manage/PersonalData";

		public virtual Task<IActionResult> OnGet() => throw new NotImplementedException();
	}

	public class PersonalDataModel<TUser> : PersonalDataModelBase where TUser : class
	{
		private readonly UserManager<TUser> _userManager;
		private readonly ILogger<PersonalDataModel<TUser>> _logger;

		public PersonalDataModel(UserManager<TUser> userManager, ILogger<PersonalDataModel<TUser>> logger)
		{
			_userManager = userManager;
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
	}

	public class PersonalDataModel : PersonalDataModel<ApplicationUser>
	{
		public PersonalDataModel(UserManager<ApplicationUser> userManager,
			ILogger<PersonalDataModel<ApplicationUser>> logger) : base(userManager, logger)
		{
		}
	}
}
