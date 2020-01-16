using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EFGetStarted.AspNetCore.ExistingDb
{
	/// <summary>
	/// Interface used to store instances of <see cref="DataProtectionKey"/> in a <see cref="DbContext"/>
	/// </summary>
	public interface IDataProtectionKeyContext
	{
		/// <summary>
		/// A collection of <see cref="DataProtectionKey"/>
		/// </summary>
		DbSet<DataProtectionKey> DataProtectionKeys { get; }
	}

	/// <summary>
	/// An <see cref="IXmlRepository"/> backed by an EntityFrameworkCore datastore.
	/// </summary>
	public class EntityFrameworkCoreXmlRepository<TContext> : IXmlRepository
		where TContext : DbContext, IDataProtectionKeyContext
	{
		private readonly IServiceProvider _services;
		private readonly ILogger _logger;

		/// <summary>
		/// Creates a new instance of the <see cref="EntityFrameworkCoreXmlRepository{TContext}"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
		public EntityFrameworkCoreXmlRepository(IServiceProvider services, ILoggerFactory loggerFactory)
		{
			if (loggerFactory == null)
			{
				throw new ArgumentNullException(nameof(loggerFactory));
			}

			_logger = loggerFactory.CreateLogger<EntityFrameworkCoreXmlRepository<TContext>>();
			_services = services ?? throw new ArgumentNullException(nameof(services));
		}

		/// <inheritdoc />
		public virtual IReadOnlyCollection<XElement> GetAllElements()
		{
			using (var scope = _services.CreateScope())
			{
				var context = scope.ServiceProvider.GetRequiredService<TContext>();
				var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
				if (env == null)
					throw new ArgumentNullException(nameof(IWebHostEnvironment));

				string environment_like = $"{env.EnvironmentName}_%";

				// Put logger in a local such that `this` isn't captured.
				var logger = _logger; 
				var found_keys = context.DataProtectionKeys.AsNoTracking()
					.Where(w => EF.Functions.Like(w.FriendlyName, environment_like))
					.Select(key => TryParseKeyXml(key.Xml, logger)).ToList().AsReadOnly();
				return found_keys;
			}
		}

		/// <inheritdoc />
		public void StoreElement(XElement element, string friendlyName)
		{
			using (var scope = _services.CreateScope())
			{
				var context = scope.ServiceProvider.GetRequiredService<TContext>();
				var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
				if (env == null)
					throw new ArgumentNullException(nameof(IWebHostEnvironment));

				if (!Enum.TryParse<EnvEnum>(env.EnvironmentName, true, out var env_enum))
					throw new NotSupportedException("bad env parsing");

				var newKey = new DataProtectionKey()
				{
					FriendlyName = $"{env.EnvironmentName}_{friendlyName}",
					Xml = element.ToString(SaveOptions.DisableFormatting),
					// Environment = env_enum,
				};

				context.DataProtectionKeys.Add(newKey);
				_logger.LogSavingKeyToDbContext(friendlyName, typeof(TContext).Name);
				context.SaveChanges();
			}
		}

		private static XElement TryParseKeyXml(string xml, ILogger logger)
		{
			try
			{
				return XElement.Parse(xml);
			}
			catch (Exception e)
			{
				logger?.LogExceptionWhileParsingKeyXml(xml, e);
				return null;
			}
		}
	}

	#region Extensions

	public static class EntityFrameworkCoreDataProtectionExtensions
	{
		/// <summary>
		/// Configures the data protection system to persist keys to an EntityFrameworkCore datastore
		/// </summary>
		/// <param name="builder">The <see cref="IDataProtectionBuilder"/> instance to modify.</param>
		/// <returns>The value <paramref name="builder"/>.</returns>
		public static IDataProtectionBuilder PersistKeysToDbContext<TContext>(this IDataProtectionBuilder builder)
			where TContext : DbContext, IDataProtectionKeyContext
		{
			builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
			{
				var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
				return new ConfigureOptions<KeyManagementOptions>(options =>
				{
					options.XmlRepository = new EntityFrameworkCoreXmlRepository<TContext>(services, loggerFactory);
				});
			});

			return builder;
		}
	}

	internal static class LoggingExtensions
	{
		private static readonly Action<ILogger, string, Exception> _anExceptionOccurredWhileParsingKeyXml;
		private static readonly Action<ILogger, string, string, Exception> _savingKeyToDbContext;

		static LoggingExtensions()
		{
			_anExceptionOccurredWhileParsingKeyXml = LoggerMessage.Define<string>(
				eventId: 1,
				logLevel: LogLevel.Warning,
				formatString: "An exception occurred while parsing the key xml '{Xml}'.");
			_savingKeyToDbContext = LoggerMessage.Define<string, string>(
				eventId: 2,
				logLevel: LogLevel.Debug,
				formatString: "Saving key '{FriendlyName}' to '{DbContext}'.");
		}

		public static void LogExceptionWhileParsingKeyXml(this ILogger logger, string keyXml, Exception exception)
			=> _anExceptionOccurredWhileParsingKeyXml(logger, keyXml, exception);

		public static void LogSavingKeyToDbContext(this ILogger logger, string friendlyName, string contextName)
			=> _savingKeyToDbContext(logger, friendlyName, contextName, null);
	}

	#endregion Extensions
}
