using InkBall.Module.Model;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace DotnetPlayground.Models
{
	// Add profile data for application users by adding properties to the ApplicationUser class
	public class ApplicationUser : IdentityUser, INamedAgedUser
	{
		static readonly JsonSerializerOptions _serializationOpts = new JsonSerializerOptions { IgnoreNullValues = true };

		[ProtectedPersonalData]
		public string Name { get; set; }

		///// <summary>
		///// TODO: Change this to birth DateTime.
		///// </summary>
		//[PersonalData]
		//public int Age { get; set; }

		/// <summary>
		/// TODO: make this a json string in DBase, optimize serialization (or drop it?)
		/// </summary>
		[PersonalData]
		[NotMapped]
		public IApplicationUserSettings UserSettings
		{
			get
			{
				return JsonSerializer.Deserialize<ApplicationUserSettings>(UserSettingsJSON ?? "{}",
					_serializationOpts);
			}
			set
			{
				UserSettingsJSON = JsonSerializer.Serialize(value, _serializationOpts);
			}
		}

		public string UserSettingsJSON { get; set; }
	}
}
