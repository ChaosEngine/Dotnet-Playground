namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	/// <summary>
	/// Environment enum
	/// </summary>
	public enum EnvEnum
	{
		DEVELOPMENT = 0,
		PRODUCTION
	}

	/// <summary>
	/// Code first model used by <see cref="EntityFrameworkCoreXmlRepository{TContext}"/>.
	/// </summary>
	public class DataProtectionKey
	{
		/// <summary>
		/// The entity identifier of the <see cref="DataProtectionKey"/>.
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// The friendly name of the <see cref="DataProtectionKey"/>.
		/// </summary>
		public string FriendlyName { get; set; }

		/// <summary>
		/// The XML representation of the <see cref="DataProtectionKey"/>.
		/// </summary>
		public string Xml { get; set; }

		/// <summary>
		/// The environment
		/// </summary>
		public EnvEnum Environment { get; set; }
	}
}
