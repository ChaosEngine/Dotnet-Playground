using Microsoft.EntityFrameworkCore;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public class GoogleProtectionKey
	{
		/// <summary>
		/// The entity identifier of the <see cref="DataProtectionKey"/>.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// The XML representation of the <see cref="DataProtectionKey"/>.
		/// </summary>
		public string Xml { get; set; }

		/// <summary>
		/// The environment
		/// </summary>
		public EnvEnum Environment { get; set; }
	}

	public interface IGoogleKeyContext
	{
		/// <summary>
		/// A collection of <see cref="DataProtectionKey"/>
		/// </summary>
		DbSet<GoogleProtectionKey> GoogleProtectionKeys { get; }
	}
}
