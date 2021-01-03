using Microsoft.EntityFrameworkCore;

namespace AspNetCore.ExistingDb.Models
{
	public sealed class GoogleProtectionKey
	{
		/// <summary>
		/// The entity identifier of the the protected key.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// The JSON representation of the protected key.
		/// </summary>
		public string Json { get; set; }

		/// <summary>
		/// The environment
		/// </summary>
		public string Environment { get; set; }
	}

	public interface IGoogleKeyContext
	{
		/// <summary>
		/// A collection of protected keys (usually only one).
		/// </summary>
		DbSet<GoogleProtectionKey> GoogleProtectionKeys { get; }
	}
}
