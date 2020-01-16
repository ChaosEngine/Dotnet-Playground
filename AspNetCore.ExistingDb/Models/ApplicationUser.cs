using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using InkBall.Module.Model;
using Microsoft.AspNetCore.Identity;

namespace IdentitySample.DefaultUI.Data
{
	// Add profile data for application users by adding properties to the ApplicationUser class
	public class ApplicationUser : IdentityUser, INamedAgedUser
	{
		[ProtectedPersonalData]
		public string Name { get; set; }

		[PersonalData]
		public int Age { get; set; }

		[PersonalData]
		[NotMapped]
		public IApplicationUserSettings UserSettings
		{
			get
			{
				return JsonSerializer.Deserialize<ApplicationUserSettings>(UserSettingsJSON ?? "{}",
					new JsonSerializerOptions { IgnoreNullValues = true });
			}
			set
			{
				UserSettingsJSON = JsonSerializer.Serialize(value,
					new JsonSerializerOptions { IgnoreNullValues = true });
			}
		}

		public string UserSettingsJSON { get; set; }
	}
}
