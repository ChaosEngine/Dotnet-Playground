using DotnetPlayground;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if INCLUDE_ORACLE
using Oracle.ManagedDataAccess.Client;
#endif
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace DotnetPlayground
{
	public class ContextFactory
	{
		/// <summary>
		/// Sets up DB kind and connection
		/// </summary>
		/// <returns>connection string</returns>
		/// <param name="dbContextOpts"></param>
		/// <param name="configuration"></param>
		/// <param name="distributedCacheServices"></param>
		internal static string ConfigureDBKind(DbContextOptionsBuilder dbContextOpts, IConfiguration configuration, IServiceCollection distributedCacheServices = null)
		{
			string conn_str;
			switch (configuration["DBKind"]?.ToLower())
			{
				case "mysql":
				case "mariadb":
				case "maria":
					conn_str = configuration.GetConnectionString("MySQL");
					if (dbContextOpts != null)
						dbContextOpts.UseMySql(conn_str, MySqlServerVersion.LatestSupportedServerVersion);
					if (distributedCacheServices != null)
					{
						distributedCacheServices.AddDistributedMySqlCache(opts =>
						{
							opts.ConnectionString = conn_str;

							var builder = new DbConnectionStringBuilder();
							builder.ConnectionString = conn_str;
							opts.SchemaName = builder["database"] as string;

							opts.TableName = nameof(SessionCache);
							opts.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
						});
					}
					break;

#if INCLUDE_SQLSERVER
				case "sqlserver":
				case "mssql":
					conn_str = configuration.GetConnectionString("SqlServer");
					if (dbContextOpts != null)
						dbContextOpts.UseSqlServer(conn_str);
					if (distributedCacheServices != null)
					{
						distributedCacheServices.AddDistributedSqlServerCache(opts =>
						{
							opts.ConnectionString = conn_str;
							opts.SchemaName = "dbo";
							opts.TableName = nameof(SessionCache);
							opts.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
						});
					}
					break;
#endif

				case "sqlite":
					conn_str = configuration.GetConnectionString("Sqlite");
					if (dbContextOpts != null)
						dbContextOpts.UseSqlite(conn_str);
					break;

#if INCLUDE_POSTGRES
				case "psql":
				case "npsql":
				case "postgres":
				case "postgresql":
					conn_str = configuration.GetConnectionString("PostgreSql");
					if (dbContextOpts != null)
					{
						var conn = new Npgsql.NpgsqlConnection(conn_str);
						conn.ProvideClientCertificatesCallback = MyProvideClientCertificatesCallback;

						dbContextOpts.UseNpgsql(conn);
					}
					break;
#endif

#if INCLUDE_ORACLE
				case "oracle":
					conn_str = configuration.GetConnectionString("Oracle");

					if (dbContextOpts != null)
					{
						if (string.IsNullOrEmpty(OracleConfiguration.TnsAdmin))
						{
							//WALLET_LOCATION=(SOURCE=(METHOD=file)(METHOD_DATA=(DIRECTORY=c:\\Users\\user\\.blablabla\\wallet)))
							string[] tab = conn_str
								.Replace("\r", string.Empty)
								.Replace("\n", string.Empty)
								.Replace(")", string.Empty)
								.Replace(" =", "=")
								.Split("WALLET_LOCATION=", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
							if (tab.Length > 1)
							{
								tab = tab[1].Split("DIRECTORY=", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
								if (tab.Length > 0)
								{
									string directory = tab[1];
									if (!string.IsNullOrEmpty(directory))
									{
										OracleConfiguration.TnsAdmin = directory;
										OracleConfiguration.WalletLocation = OracleConfiguration.TnsAdmin;
									}
								}
							}
						}

						dbContextOpts.UseOracle(conn_str);
					}
					break;
#endif

				default:
					throw new NotSupportedException($"Bad DBKind name {configuration["DBKind"]?.ToLower()}");
			}
			return conn_str;
		}

		protected Dictionary<string, string> GetConnStringAsDictionary(string connectionString)
		{
			Dictionary<string, string> dict =
				Regex.Matches(connectionString, @"\s*(?<key>[^;=]+)\s*=\s*((?<value>[^'][^;]*)|'(?<value>[^']*)')")
				.Cast<Match>()
				.ToDictionary(m => m.Groups["key"].Value, m => m.Groups["value"].Value);
			return dict;
		}

		internal static void MyProvideClientCertificatesCallback(X509CertificateCollection clientCerts)
		{
			using (X509Store store = new X509Store(StoreLocation.CurrentUser))
			{
				store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

				var currentCerts = store.Certificates;
				currentCerts = currentCerts.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
				currentCerts = currentCerts.Find(X509FindType.FindByIssuerName, "theBrain.ca", false);
				currentCerts = currentCerts.Find(X509FindType.FindBySubjectName, Environment.MachineName, false);
				if (currentCerts != null && currentCerts.Count > 0)
				{
					var cert = currentCerts[0];
					clientCerts.Add(cert);
				}
			}
		}

		public IConfiguration GetConfiguration(string[] args)
		{
			var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

			// Used only for EF .NET Core CLI tools (update database/migrations etc.)
			var builder = new ConfigurationBuilder()
				.SetBasePath(Path.Combine(Directory.GetCurrentDirectory()))
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env}.json", optional: true)
				.AddEnvironmentVariables();
			if (string.IsNullOrEmpty(env) || env == "Development")
				builder.AddUserSecrets<Startup>();
			var config = builder.Build();

			return config;
		}
	}
}
