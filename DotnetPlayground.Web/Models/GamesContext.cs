using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotnetPlayground.Models
{
	public class GamesContextFactory : ContextFactory, IDesignTimeDbContextFactory<InkBall.Module.Model.GamesContext>
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
		public InkBall.Module.Model.GamesContext CreateDbContext(string[] args)
		{
			var configuration = GetConfiguration(args);

			var optionsBuilder = new DbContextOptionsBuilder<InkBall.Module.Model.GamesContext>();

			ConfigureDBKind(optionsBuilder, configuration, null);

			return new InkBall.Module.Model.GamesContext(optionsBuilder.Options);
		}
	}
}
