using InkBall.Module.Model;
using Microsoft.AspNetCore.Identity;

namespace IdentitySample.DefaultUI.Data
{
	/* public sealed class ApplicationUserSettings : IApplicationUserSettings
	{
		//public int ID { get; set; }
		public bool DesktopNotifications { get; set; }
	}*/

	// Add profile data for application users by adding properties to the ApplicationUser class
	public class ApplicationUser : IdentityUser, INamedAgedUser
	{
		[ProtectedPersonalData]
		public string Name { get; set; }

		[PersonalData]
		public int Age { get; set; }

		[PersonalData]
		public IApplicationUserSettings UserSettings { get; set; }
	}
}
