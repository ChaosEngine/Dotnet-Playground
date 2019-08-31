using System.ComponentModel.DataAnnotations.Schema;
using InkBall.Module.Model;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

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
				return JsonConvert.DeserializeObject<ApplicationUserSettings>(UserSettingsJSON ?? "",
					new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
			}
			set
			{
				UserSettingsJSON = JsonConvert.SerializeObject(value,
					new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
			}
		}

		public string UserSettingsJSON { get; set; }
	}
}
