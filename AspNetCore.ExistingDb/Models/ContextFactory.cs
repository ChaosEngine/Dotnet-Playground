using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using AspNetCore.ExistingDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
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
						dbContextOpts.UseMySql(conn_str);
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

				case "sqlite":
					conn_str = configuration.GetConnectionString("Sqlite");
					if (dbContextOpts != null)
						dbContextOpts.UseSqlite(conn_str);
					break;

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

				default:
					throw new NotSupportedException($"Bad DBKind name");
			}
			return conn_str;
		}

		private Dictionary<string, string> GetConnStringAsDictionary(string connectionString)
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
	}
}
