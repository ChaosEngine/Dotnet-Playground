
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityManager2;
using IdentityManager2.Core;
using IdentityManager2.Core.Metadata;
using IdentityManager2.Extensions;
using Microsoft.AspNetCore.Identity;

namespace DotnetPlayground.Services;

public class MyAspNetCoreIdentityManagerService<TUser, TUserKey, TRole, TRoleKey> : IIdentityManagerService where TUser : IdentityUser<TUserKey>, new() where TUserKey : IEquatable<TUserKey> where TRole : IdentityRole<TRoleKey>, new() where TRoleKey : IEquatable<TRoleKey>
{
	protected readonly UserManager<TUser> UserManager;

	protected readonly RoleManager<TRole> RoleManager;

	protected readonly Func<Task<IdentityManagerMetadata>> MetadataFunc;

	public string RoleClaimType { get; set; }

	internal MyAspNetCoreIdentityManagerService(UserManager<TUser> userManager, RoleManager<TRole> roleManager)
	{
		UserManager = userManager ?? throw new ArgumentNullException("userManager");
		RoleManager = roleManager ?? throw new ArgumentNullException("roleManager");
		if (!userManager.SupportsQueryableUsers)
		{
			throw new InvalidOperationException("UserManager must support queryable users.");
		}

		string emailConfirmationTokenProvider = userManager.Options.Tokens.EmailConfirmationTokenProvider;
		userManager.Options.Tokens.ProviderMap.ContainsKey(emailConfirmationTokenProvider);
		RoleClaimType = "role";
	}

	public MyAspNetCoreIdentityManagerService(UserManager<TUser> userManager, RoleManager<TRole> roleManager, bool includeAccountProperties = true)
		: this(userManager, roleManager)
	{
		MyAspNetCoreIdentityManagerService<TUser, TUserKey, TRole, TRoleKey> aspNetCoreIdentityManagerService = this;
		MetadataFunc = () => aspNetCoreIdentityManagerService.GetStandardMetadata(includeAccountProperties);
	}

	public MyAspNetCoreIdentityManagerService(UserManager<TUser> userManager, RoleManager<TRole> roleManager, IdentityManagerMetadata metadata)
		: this(userManager, roleManager, (Func<Task<IdentityManagerMetadata>>)(() => Task.FromResult<IdentityManagerMetadata>(metadata)))
	{
	}

	public MyAspNetCoreIdentityManagerService(UserManager<TUser> userManager, RoleManager<TRole> roleManager, Func<Task<IdentityManagerMetadata>> metadataFunc)
		: this(userManager, roleManager)
	{
		MetadataFunc = metadataFunc;
	}

	public Task<IdentityManagerMetadata> GetMetadataAsync()
	{
		return MetadataFunc();
	}

	public async Task<IdentityManagerResult<CreateResult>> CreateUserAsync(IEnumerable<PropertyValue> properties)
	{
		PropertyValue val = properties.Single((PropertyValue x) => x.Type == "username");
		PropertyValue val2 = properties.Single((PropertyValue x) => x.Type == "password");
		string username = val.Value;
		string password = val2.Value;
		string[] exclude = new string[2] { "username", "password" };
		PropertyValue[] otherProperties = properties.Where((PropertyValue x) => !exclude.Contains(x.Type)).ToArray();
		IEnumerable<PropertyMetadata> createProps = UserMetadataExtensions.GetCreateProperties((await GetMetadataAsync()).UserMetadata);
		TUser user = new TUser
		{
			UserName = username
		};
		PropertyValue[] array = otherProperties;
		foreach (PropertyValue val3 in array)
		{
			IdentityManagerResult val4 = await SetUserProperty(createProps, user, val3.Type, val3.Value);
			if (!val4.IsSuccess)
			{
				return new IdentityManagerResult<CreateResult>(val4.Errors.ToArray());
			}
		}

		IdentityResult identityResult = await UserManager.CreateAsync(user, password);
		if (!identityResult.Succeeded)
		{
			return new IdentityManagerResult<CreateResult>(identityResult.Errors.Select((IdentityError x) => x.Description).ToArray());
		}

		return new IdentityManagerResult<CreateResult>(new CreateResult
		{
			Subject = user.Id.ToString()
		});
	}

	public async Task<IdentityManagerResult> DeleteUserAsync(string subject)
	{
		TUser val = await UserManager.FindByIdAsync(subject);
		if (val == null)
		{
			return new IdentityManagerResult(new string[1] { "Invalid subject" });
		}

		IdentityResult identityResult = await UserManager.DeleteAsync(val);
		if (!identityResult.Succeeded)
		{
			return (IdentityManagerResult)(object)new IdentityManagerResult<CreateResult>(identityResult.Errors.Select((IdentityError x) => x.Description).ToArray());
		}

		return IdentityManagerResult.Success;
	}

	public async Task<IdentityManagerResult<QueryResult<UserSummary>>> QueryUsersAsync(string filter, int start, int count)
	{
		IOrderedQueryable<TUser> source = UserManager.Users.OrderBy((TUser user) => user.UserName);
		if (!string.IsNullOrWhiteSpace(filter))
		{
			source = from user in source
					 where user.UserName.Contains(filter)
					 orderby user.UserName
					 select user;
		}

		int total = source.Count();
		TUser[] array = source.Skip(start).Take(count).ToArray();
		List<UserSummary> items = new List<UserSummary>();
		TUser[] array2 = array;
		foreach (TUser val in array2)
		{
			List<UserSummary> list = items;
			UserSummary val2 = new UserSummary();
			val2.Subject = val.Id.ToString();
			val2.Username = val.UserName;
			UserSummary val3 = val2;
			val3.Name = await DisplayNameFromUser(val);
			list.Add(val2);
		}

		return new IdentityManagerResult<QueryResult<UserSummary>>(new QueryResult<UserSummary>
		{
			Start = start,
			Count = count,
			Total = total,
			Filter = filter,
			Items = items
		});
	}

	public async Task<IdentityManagerResult<UserDetail>> GetUserAsync(string subject)
	{
		TUser user = await UserManager.FindByIdAsync(subject);
		if (user == null)
		{
			return new IdentityManagerResult<UserDetail>((UserDetail)null);
		}

		UserDetail val = new UserDetail();
		((UserSummary)val).Subject = subject;
		((UserSummary)val).Username = user.UserName;
		UserDetail val2 = val;
		((UserSummary)val2).Name = await DisplayNameFromUser(user);
		UserDetail result = val;
		IdentityManagerMetadata val3 = await GetMetadataAsync();
		List<PropertyValue> props = new List<PropertyValue>();
		foreach (PropertyMetadata updateProperty in val3.UserMetadata.UpdateProperties)
		{
			List<PropertyValue> list = props;
			PropertyValue val4 = new PropertyValue();
			val4.Type = updateProperty.Type;
			PropertyValue val5 = val4;
			val5.Value = await GetUserProperty(updateProperty, user);
			list.Add(val4);
		}

		result.Properties = props.ToArray();
		if (UserManager.SupportsUserClaim)
		{
			IList<Claim> list2 = await UserManager.GetClaimsAsync(user);
			List<ClaimValue> list3 = new List<ClaimValue>();
			if (list2 != null)
			{
				list3.AddRange(((IEnumerable<Claim>)list2).Select((Func<Claim, ClaimValue>)((Claim x) => new ClaimValue
				{
					Type = x.Type,
					Value = x.Value
				})));
			}

			result.Claims = list3.ToArray();
		}

		return new IdentityManagerResult<UserDetail>(result);
	}

	public async Task<IdentityManagerResult> SetUserPropertyAsync(string subject, string type, string value)
	{
		TUser user = await UserManager.FindByIdAsync(subject);
		if (user == null)
		{
			return new IdentityManagerResult(new string[1] { "Invalid subject" });
		}

		List<string> list = ValidateUserProperty(type, value).ToList();
		if (list.Any())
		{
			return new IdentityManagerResult(list.ToArray());
		}

		IdentityManagerResult val = await SetUserProperty((await GetMetadataAsync()).UserMetadata.UpdateProperties, user, type, value);
		if (!val.IsSuccess)
		{
			return val;
		}

		IdentityResult identityResult = await UserManager.UpdateAsync(user);
		if (!identityResult.Succeeded)
		{
			return new IdentityManagerResult(identityResult.Errors.Select((IdentityError x) => x.Description).ToArray());
		}

		return IdentityManagerResult.Success;
	}

	public async Task<IdentityManagerResult> AddUserClaimAsync(string subject, string type, string value)
	{
		TUser user = await UserManager.FindByIdAsync(subject);
		if (user == null)
		{
			return new IdentityManagerResult(new string[1] { "Invalid subject" });
		}

		if (!(await UserManager.GetClaimsAsync(user)).Any((Claim x) => x.Type == type && x.Value == value))
		{
			IdentityResult identityResult = await UserManager.AddClaimAsync(user, new Claim(type, value));
			if (!identityResult.Succeeded)
			{
				return (IdentityManagerResult)(object)new IdentityManagerResult<CreateResult>(identityResult.Errors.Select((IdentityError x) => x.Description).ToArray());
			}
		}

		return IdentityManagerResult.Success;
	}

	public async Task<IdentityManagerResult> RemoveUserClaimAsync(string subject, string type, string value)
	{
		TUser val = await UserManager.FindByIdAsync(subject);
		if (val == null)
		{
			return new IdentityManagerResult(new string[1] { "Invalid subject" });
		}

		IdentityResult identityResult = await UserManager.RemoveClaimAsync(val, new Claim(type, value));
		if (!identityResult.Succeeded)
		{
			return (IdentityManagerResult)(object)new IdentityManagerResult<CreateResult>(identityResult.Errors.Select((IdentityError x) => x.Description).ToArray());
		}

		return IdentityManagerResult.Success;
	}

	public async Task<IdentityManagerResult<CreateResult>> CreateRoleAsync(IEnumerable<PropertyValue> properties)
	{
		ValidateSupportsRoles();
		PropertyValue val = properties.Single((PropertyValue x) => x.Type == "name");
		string name = val.Value;
		string[] exclude = new string[1] { "name" };
		PropertyValue[] otherProperties = properties.Where((PropertyValue x) => !exclude.Contains(x.Type)).ToArray();
		IEnumerable<PropertyMetadata> createProps = RoleMetadataExtensions.GetCreateProperties((await GetMetadataAsync()).RoleMetadata);
		TRole role = new TRole
		{
			Name = name
		};
		PropertyValue[] array = otherProperties;
		foreach (PropertyValue val2 in array)
		{
			IdentityManagerResult val3 = await SetRoleProperty(createProps, role, val2.Type, val2.Value);
			if (!val3.IsSuccess)
			{
				return new IdentityManagerResult<CreateResult>(val3.Errors.ToArray());
			}
		}

		IdentityResult identityResult = await RoleManager.CreateAsync(role);
		if (!identityResult.Succeeded)
		{
			return new IdentityManagerResult<CreateResult>(identityResult.Errors.Select((IdentityError x) => x.Description).ToArray());
		}

		return new IdentityManagerResult<CreateResult>(new CreateResult
		{
			Subject = role.Id.ToString()
		});
	}

	public async Task<IdentityManagerResult> DeleteRoleAsync(string subject)
	{
		ValidateSupportsRoles();
		TRole val = await RoleManager.FindByIdAsync(subject);
		if (val == null)
		{
			return new IdentityManagerResult(new string[1] { "Invalid subject" });
		}

		IdentityResult identityResult = await RoleManager.DeleteAsync(val);
		if (!identityResult.Succeeded)
		{
			return (IdentityManagerResult)(object)new IdentityManagerResult<CreateResult>(identityResult.Errors.Select((IdentityError x) => x.Description).ToArray());
		}

		return IdentityManagerResult.Success;
	}

	public Task<IdentityManagerResult<QueryResult<RoleSummary>>> QueryRolesAsync(string filter, int start, int count)
	{
		ValidateSupportsRoles();
		if (start < 0)
		{
			start = 0;
		}

		if (count < 0)
		{
			count = int.MaxValue;
		}

		IOrderedQueryable<TRole> source = RoleManager.Roles.OrderBy((TRole role) => role.Name);
		if (!string.IsNullOrWhiteSpace(filter))
		{
			source = from role in source
					 where role.Name.Contains(filter)
					 orderby role.Name
					 select role;
		}

		int total = source.Count();
		TRole[] source2 = source.Skip(start).Take(count).ToArray();
		return Task.FromResult<IdentityManagerResult<QueryResult<RoleSummary>>>(new IdentityManagerResult<QueryResult<RoleSummary>>(new QueryResult<RoleSummary>
		{
			Start = start,
			Count = count,
			Total = total,
			Filter = filter,
			Items = ((IEnumerable<TRole>)source2).Select((Func<TRole, RoleSummary>)((TRole x) => new RoleSummary
			{
				Subject = x.Id.ToString(),
				Name = x.Name
			})).ToArray()
		}));
	}

	public async Task<IdentityManagerResult<RoleDetail>> GetRoleAsync(string subject)
	{
		ValidateSupportsRoles();
		TRole role = await RoleManager.FindByIdAsync(subject);
		if (role == null)
		{
			return new IdentityManagerResult<RoleDetail>((RoleDetail)null);
		}

		RoleDetail result = new RoleDetail
		{
			Subject = subject,
			Name = role.Name
		};
		IdentityManagerMetadata val = await GetMetadataAsync();
		List<PropertyValue> props = new List<PropertyValue>();
		foreach (PropertyMetadata updateProperty in val.RoleMetadata.UpdateProperties)
		{
			List<PropertyValue> list = props;
			PropertyValue val2 = new PropertyValue();
			val2.Type = updateProperty.Type;
			PropertyValue val3 = val2;
			val3.Value = await GetRoleProperty(updateProperty, role);
			list.Add(val2);
		}

		result.Properties = props.ToArray();
		return new IdentityManagerResult<RoleDetail>(result);
	}

	public async Task<IdentityManagerResult> SetRolePropertyAsync(string subject, string type, string value)
	{
		ValidateSupportsRoles();
		TRole role = await RoleManager.FindByIdAsync(subject);
		if (role == null)
		{
			return new IdentityManagerResult(new string[1] { "Invalid subject" });
		}

		List<string> list = ValidateRoleProperty(type, value).ToList();
		if (list.Any())
		{
			return new IdentityManagerResult(list.ToArray());
		}

		IdentityManagerResult result = await SetRoleProperty((await GetMetadataAsync()).RoleMetadata.UpdateProperties, role, type, value);
		if (!result.IsSuccess)
		{
			return result;
		}

		if (!(await RoleManager.UpdateAsync(role)).Succeeded)
		{
			return new IdentityManagerResult(result.Errors.ToArray());
		}

		return IdentityManagerResult.Success;
	}

	public virtual Task<IdentityManagerMetadata> GetStandardMetadata(bool includeAccountProperties = true)
	{
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c0: Expected O, but got Unknown
		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Expected O, but got Unknown
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Expected O, but got Unknown
		List<PropertyMetadata> list = new List<PropertyMetadata>();
		if (UserManager.SupportsUserPassword)
		{
			list.Add(PropertyMetadata.FromFunctions<TUser, string>("password", (Func<TUser, Task<string>>)((TUser u) => Task.FromResult<string>(null)), (Func<TUser, string, Task<IdentityManagerResult>>)SetPassword, "Password", (PropertyDataType?)(PropertyDataType)1, (bool?)true));
		}

		if (UserManager.SupportsUserEmail)
		{
			list.Add(PropertyMetadata.FromFunctions<TUser, string>("email", (Func<TUser, Task<string>>)((TUser u) => GetEmail(u)), (Func<TUser, string, Task<IdentityManagerResult>>)SetEmail, "Email", (PropertyDataType?)(PropertyDataType)2, (bool?)null));
		}

		if (UserManager.SupportsUserPhoneNumber)
		{
			list.Add(PropertyMetadata.FromFunctions<TUser, string>("phone", (Func<TUser, Task<string>>)((TUser u) => GetPhone(u)), (Func<TUser, string, Task<IdentityManagerResult>>)SetPhone, "Phone", (PropertyDataType?)(PropertyDataType)0, (bool?)null));
		}

		if (UserManager.SupportsUserTwoFactor)
		{
			list.Add(PropertyMetadata.FromFunctions<TUser, bool>("two_factor", (Func<TUser, Task<bool>>)((TUser u) => GetTwoFactorEnabled(u)), (Func<TUser, bool, Task<IdentityManagerResult>>)SetTwoFactorEnabled, "Two Factor Enabled", (PropertyDataType?)(PropertyDataType)5, (bool?)null));
		}

		if (UserManager.SupportsUserLockout)
		{
			list.Add(PropertyMetadata.FromFunctions<TUser, bool>("locked_enabled", (Func<TUser, Task<bool>>)GetLockoutEnabled, (Func<TUser, bool, Task<IdentityManagerResult>>)((TUser user1, bool enabled) => SetLockoutEnabled(user1, enabled)), "Lockout Enabled", (PropertyDataType?)(PropertyDataType)5, (bool?)null));
			list.Add(PropertyMetadata.FromFunctions<TUser, bool>("locked", (Func<TUser, Task<bool>>)GetLockedOut, (Func<TUser, bool, Task<IdentityManagerResult>>)((TUser user1, bool locked) => SetLockedOut(user1, locked)), "Locked Out", (PropertyDataType?)(PropertyDataType)5, (bool?)null));
		}

		if (includeAccountProperties)
		{
			list.AddRange(PropertyMetadata.FromType<TUser>());
		}

		List<PropertyMetadata> list2 = new List<PropertyMetadata>();
		list2.Add(PropertyMetadata.FromProperty<TUser>((Expression<Func<TUser, object>>)((TUser x) => x.UserName), "username", (string)null, (PropertyDataType?)null, (bool?)true));
		list2.Add(PropertyMetadata.FromFunctions<TUser, string>("password", (Func<TUser, Task<string>>)((TUser u) => Task.FromResult<string>(null)), (Func<TUser, string, Task<IdentityManagerResult>>)SetPassword, "Password", (PropertyDataType?)(PropertyDataType)1, (bool?)true));
		UserMetadata userMetadata = new UserMetadata
		{
			SupportsCreate = true,
			SupportsDelete = true,
			SupportsClaims = UserManager.SupportsUserClaim,
			CreateProperties = list2,
			UpdateProperties = list
		};
		RoleMetadata val = new RoleMetadata();
		val.RoleClaimType = RoleClaimType;
		val.SupportsCreate = true;
		val.SupportsDelete = true;
		val.CreateProperties = (IEnumerable<PropertyMetadata>)(object)new PropertyMetadata[1] { PropertyMetadata.FromProperty<TRole>((Expression<Func<TRole, object>>)((TRole x) => x.Name), "name", (string)null, (PropertyDataType?)null, (bool?)true) };
		RoleMetadata roleMetadata = val;
		return Task.FromResult<IdentityManagerMetadata>(new IdentityManagerMetadata
		{
			UserMetadata = userMetadata,
			RoleMetadata = roleMetadata
		});
	}

	public virtual PropertyMetadata GetMetadataForClaim(string type, string name = null, PropertyDataType dataType = (PropertyDataType)0, bool required = false)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return PropertyMetadata.FromFunctions<TUser, string>(type, GetForClaim(type), SetForClaim(type), name, (PropertyDataType?)dataType, (bool?)required);
	}

	public virtual Func<TUser, Task<string>> GetForClaim(string type)
	{
		return async (TUser user) => (from x in await UserManager.GetClaimsAsync(user)
									  where x.Type == type
									  select x.Value).FirstOrDefault();
	}

	public virtual Func<TUser, string, Task<IdentityManagerResult>> SetForClaim(string type)
	{
		return async delegate (TUser user, string value)
		{
			IList<Claim> list = (await UserManager.GetClaimsAsync(user)).Where((Claim x) => x.Type == type).ToArray();
			foreach (Claim item in list)
			{
				IdentityResult identityResult = await UserManager.RemoveClaimAsync(user, item);
				if (!identityResult.Succeeded)
				{
					return new IdentityManagerResult(new string[1] { identityResult.Errors.First().Description });
				}
			}

			if (!string.IsNullOrWhiteSpace(value))
			{
				IdentityResult identityResult2 = await UserManager.AddClaimAsync(user, new Claim(type, value));
				if (!identityResult2.Succeeded)
				{
					return new IdentityManagerResult(new string[1] { identityResult2.Errors.First().Description });
				}
			}

			return IdentityManagerResult.Success;
		};
	}

	public virtual async Task<IdentityManagerResult> SetPassword(TUser user, string password)
	{
		string token = await UserManager.GeneratePasswordResetTokenAsync(user);
		IdentityResult identityResult = await UserManager.ResetPasswordAsync(user, token, password);
		if (!identityResult.Succeeded)
		{
			return new IdentityManagerResult(new string[1] { identityResult.Errors.First().Description });
		}

		return IdentityManagerResult.Success;
	}

	public virtual async Task<IdentityManagerResult> SetUsername(TUser user, string username)
	{
		IdentityResult identityResult = await UserManager.SetUserNameAsync(user, username);
		if (!identityResult.Succeeded)
		{
			return new IdentityManagerResult(new string[1] { identityResult.Errors.First().Description });
		}

		return IdentityManagerResult.Success;
	}

	public virtual Task<string> GetEmail(TUser user)
	{
		return UserManager.GetEmailAsync(user);
	}

	public virtual async Task<IdentityManagerResult> SetEmail(TUser user, string email)
	{
		IdentityResult identityResult = await UserManager.SetEmailAsync(user, email);
		if (!identityResult.Succeeded)
		{
			return new IdentityManagerResult(new string[1] { identityResult.Errors.First().Description });
		}

		if (!string.IsNullOrWhiteSpace(email))
		{
			string token = await UserManager.GenerateEmailConfirmationTokenAsync(user);
			identityResult = await UserManager.ConfirmEmailAsync(user, token);
			if (!identityResult.Succeeded)
			{
				return new IdentityManagerResult(new string[1] { identityResult.Errors.First().Description });
			}
		}

		return IdentityManagerResult.Success;
	}

	public virtual Task<string> GetPhone(TUser user)
	{
		return UserManager.GetPhoneNumberAsync(user);
	}

	public virtual async Task<IdentityManagerResult> SetPhone(TUser user, string phone)
	{
		IdentityResult identityResult = await UserManager.SetPhoneNumberAsync(user, phone);
		if (!identityResult.Succeeded)
		{
			return new IdentityManagerResult(new string[1] { identityResult.Errors.First().Description });
		}

		if (!string.IsNullOrWhiteSpace(phone))
		{
			string token = await UserManager.GenerateChangePhoneNumberTokenAsync(user, phone);
			identityResult = await UserManager.ChangePhoneNumberAsync(user, phone, token);
			if (!identityResult.Succeeded)
			{
				return new IdentityManagerResult(new string[1] { identityResult.Errors.First().Description });
			}
		}

		return IdentityManagerResult.Success;
	}

	public virtual Task<bool> GetTwoFactorEnabled(TUser user)
	{
		return UserManager.GetTwoFactorEnabledAsync(user);
	}

	public virtual async Task<IdentityManagerResult> SetTwoFactorEnabled(TUser user, bool enabled)
	{
		IdentityResult identityResult = await UserManager.SetTwoFactorEnabledAsync(user, enabled);
		if (!identityResult.Succeeded)
		{
			return new IdentityManagerResult(new string[1] { identityResult.Errors.First().Description });
		}

		return IdentityManagerResult.Success;
	}

	public virtual Task<bool> GetLockoutEnabled(TUser user)
	{
		return UserManager.GetLockoutEnabledAsync(user);
	}

	public virtual async Task<IdentityManagerResult> SetLockoutEnabled(TUser user, bool enabled)
	{
		IdentityResult identityResult = await UserManager.SetLockoutEnabledAsync(user, enabled);
		if (!identityResult.Succeeded)
		{
			return new IdentityManagerResult(new string[1] { identityResult.Errors.First().Description });
		}

		return IdentityManagerResult.Success;
	}

	public virtual Task<bool> GetLockedOut(TUser user)
	{
		return UserManager.IsLockedOutAsync(user);
	}

	public virtual async Task<IdentityManagerResult> SetLockedOut(TUser user, bool locked)
	{
		if (locked)
		{
			IdentityResult identityResult = await UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
			if (!identityResult.Succeeded)
			{
				return new IdentityManagerResult(new string[1] { identityResult.Errors.First().Description });
			}
		}
		else
		{
			IdentityResult identityResult2 = await UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MinValue);
			if (!identityResult2.Succeeded)
			{
				return new IdentityManagerResult(new string[1] { identityResult2.Errors.First().Description });
			}
		}

		return IdentityManagerResult.Success;
	}

	public virtual async Task<IdentityManagerResult> SetName(TRole user, string name)
	{
		IdentityResult identityResult = await RoleManager.SetRoleNameAsync(user, name);
		if (!identityResult.Succeeded)
		{
			return new IdentityManagerResult(new string[1] { identityResult.Errors.First().Description });
		}

		return IdentityManagerResult.Success;
	}

	protected virtual Task<string> GetUserProperty(PropertyMetadata propMetadata, TUser user)
	{
		Task<string> result = default(Task<string>);
		if (PropertyMetadataExtensions.TryGet(propMetadata, (object)user, out result))
		{
			return result;
		}

		throw new Exception("Invalid property type " + propMetadata.Type);
	}

	protected virtual Task<IdentityManagerResult> SetUserProperty(IEnumerable<PropertyMetadata> propsMeta, TUser user, string type, string value)
	{
		Task<IdentityManagerResult> result = default(Task<IdentityManagerResult>);
		if (PropertyMetadataExtensions.TrySet(propsMeta, (object)user, type, value, out result))
		{
			return result;
		}

		throw new Exception("Invalid property type " + type);
	}

	protected virtual async Task<string> DisplayNameFromUser(TUser user)
	{
		string text = null;

		if (user is InkBall.Module.Model.INamedAgedUser inau)
			text = inau.Name;
		else
		{
			if (UserManager.SupportsUserClaim)
			{
				text = (from x in await UserManager.GetClaimsAsync(user)
						where x.Type == "name"
						select x.Value).FirstOrDefault();
			}
		}

		if (!string.IsNullOrWhiteSpace(text))
			return text;
		return null;
	}

	protected virtual IEnumerable<string> ValidateUserProperty(string type, string value)
	{
		return Enumerable.Empty<string>();
	}

	protected virtual void ValidateSupportsRoles()
	{
		if (RoleManager == null)
		{
			throw new InvalidOperationException("Roles Not Supported");
		}
	}

	protected virtual Task<string> GetRoleProperty(PropertyMetadata propMetadata, TRole role)
	{
		Task<string> result = default(Task<string>);
		if (PropertyMetadataExtensions.TryGet(propMetadata, (object)role, out result))
		{
			return result;
		}

		throw new Exception("Invalid property type " + propMetadata.Type);
	}

	protected virtual IEnumerable<string> ValidateRoleProperty(string type, string value)
	{
		return Enumerable.Empty<string>();
	}

	protected virtual Task<IdentityManagerResult> SetRoleProperty(IEnumerable<PropertyMetadata> propsMeta, TRole role, string type, string value)
	{
		Task<IdentityManagerResult> result = default(Task<IdentityManagerResult>);
		if (PropertyMetadataExtensions.TrySet(propsMeta, (object)role, type, value, out result))
		{
			return result;
		}

		throw new Exception("Invalid property type " + type);
	}
}
