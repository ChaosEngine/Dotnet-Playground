using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IdentitySample.DefaultUI.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public class ApplicationDbContextContextFactory : ContextFactory, IDesignTimeDbContextFactory<ApplicationDbContext>
	{
		/// <summary>
		// A factory for creating derived Microsoft.EntityFrameworkCore.DbContext instances.
		// Implement this interface to enable design-time services for context types that
		// do not have a public default constructor. At design-time, derived Microsoft.EntityFrameworkCore.DbContext
		// instances can be created in order to enable specific design-time experiences
		// such as Migrations. Design-time services will automatically discover implementations
		// of this interface that are in the startup assembly or the same assembly as the
		// derived context.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public ApplicationDbContext CreateDbContext(string[] args)
		{
			var configuration = GetConfiguration(args);

			var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

			ConfigureDBKind(optionsBuilder, configuration, null);

			return new ApplicationDbContext(optionsBuilder.Options);
		}
	}

	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
			// Customize the ASP.NET Identity model and override the defaults if needed.
			// For example, you can rename the ASP.NET Identity table names and more.
			// Add your customizations after calling base.OnModelCreating(builder);
		}
	}
}
