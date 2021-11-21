using InkBall.Module;
using InkBall.Module.Model;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetPlayground.Models
{
	// Add profile data for application users by adding properties to the ApplicationUser class
	public class ApplicationUser : IdentityUser, INamedAgedUser
	{
		//static readonly JsonSerializerOptions _serializationOpts = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

		[ProtectedPersonalData]
		public string Name { get; set; }

		/// <summary>
		/// TODO: make this a json string in DBase, optimize serialization (or drop it?)
		/// </summary>
		[PersonalData]
		[NotMapped]
		public IApplicationUserSettings UserSettings
		{
			get
			{
				return JsonSerializer.Deserialize(UserSettingsJSON ?? "{}",
					IApplicationUserSettingsContext.Default.IApplicationUserSettings);
			}
			set
			{
				UserSettingsJSON = JsonSerializer.Serialize(value,
					IApplicationUserSettingsContext.Default.IApplicationUserSettings);
			}
		}

		public string UserSettingsJSON { get; set; }
	}
}
