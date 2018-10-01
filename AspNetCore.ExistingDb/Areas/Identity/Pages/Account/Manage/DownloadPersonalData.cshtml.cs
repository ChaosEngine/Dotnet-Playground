// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentitySample.DefaultUI.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IdentitySample.DefaultUI
{
	public abstract class DownloadPersonalDataModelBase : PageModel
	{
		public virtual IActionResult OnGet() => throw new NotImplementedException();

		public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();
	}

	public class DownloadPersonalDataModel<TUser> : DownloadPersonalDataModelBase where TUser : class
	{
		private readonly UserManager<TUser> _userManager;
		private readonly ILogger<DownloadPersonalDataModel<TUser>> _logger;

		public DownloadPersonalDataModel(
			UserManager<TUser> userManager,
			ILogger<DownloadPersonalDataModel<TUser>> logger)
		{
			_userManager = userManager;
			_logger = logger;
		}

		public override IActionResult OnGet()
		{
			return NotFound();
		}

		public override async Task<IActionResult> OnPostAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			_logger.LogInformation("User with ID '{UserId}' asked for their personal data.", _userManager.GetUserId(User));

			// Only include personal data for download
			var personalData = new Dictionary<string, string>();
			var personalDataProps = typeof(TUser).GetProperties().Where(
				prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
			foreach (var p in personalDataProps)
			{
				personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
			}

			var logins = await _userManager.GetLoginsAsync(user);
			foreach (var l in logins)
			{
				personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
			}

			personalData.Add($"Authenticator Key", await _userManager.GetAuthenticatorKeyAsync(user));

			Response.Headers.Add("Content-Disposition", "attachment; filename=PersonalData.json");
			return new FileContentResult(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(personalData)), "text/json");
		}
	}

	public class DownloadPersonalDataModel : DownloadPersonalDataModel<ApplicationUser>
	{
		public DownloadPersonalDataModel(
			UserManager<ApplicationUser> userManager,
			ILogger<DownloadPersonalDataModel<ApplicationUser>> logger) : base(userManager, logger)
		{

		}
	}
}
