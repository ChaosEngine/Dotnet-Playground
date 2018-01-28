using AspNetCore.ExistingDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public class BloggingContextFactory : IDesignTimeDbContextFactory<BloggingContext>
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
		public BloggingContext CreateDbContext(string[] args)
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

			var optionsBuilder = new DbContextOptionsBuilder<BloggingContext>();

			ConfigureDBKind(optionsBuilder, config, null);

			return new BloggingContext(optionsBuilder.Options);
		}
	}

	public partial class BloggingContext : DbContext
	{
		public virtual DbSet<Blog> Blogs { get; set; }
		public virtual DbSet<Post> Posts { get; set; }
		public virtual DbSet<ThinHashes> ThinHashes { get; set; }
		public virtual DbSet<HashesInfo> HashesInfo { get; set; }

		//public static bool IsMySql { get; private set; }
		public string ConnectionTypeName
		{
			get
			{
				switch (Database.ProviderName)
				{
					case "Microsoft.EntityFrameworkCore.Sqlite":
						return "sqliteconnection";
					case "Microsoft.EntityFrameworkCore.SqlServer":
						return "sqlconnection";
					case "Pomelo.EntityFrameworkCore.MySql":
						return "mysqlconnection";
					case "Npgsql.EntityFrameworkCore.PostgreSQL":
						return "npsqlconnection";
					default:
						throw new NotSupportedException($"Bad DBKind name");
				}
				//IsMySql = ConnectionTypeName == typeof(MySqlConnection).Name.ToLower();
			}
		}

		public BloggingContext(DbContextOptions<BloggingContext> options)
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Blog>(entity =>
			{
				entity.Property(e => e.BlogId).ValueGeneratedOnAdd();
				entity.Property(e => e.Url).IsRequired();
				entity.ToTable("Blog");
			});

			modelBuilder.Entity<Post>(entity =>
			{
				entity.HasOne(d => d.Blog)
					.WithMany(p => p.Post)
					.HasForeignKey(d => d.BlogId);
				entity.ToTable("Post");
			});

			modelBuilder.Entity<ThinHashes>(entity =>
			{
				if (ConnectionTypeName == "npsqlconnection")
				{
					entity.Property(e => e.Key).IsRequired().HasColumnType("varchar(20)");
					entity.Property(e => e.HashMD5).IsRequired().HasColumnType("character(32)").HasColumnName("hashMD5");
					entity.Property(e => e.HashSHA256).IsRequired().HasColumnType("character(64)").HasColumnName("hashSHA256");
				}
				else
				{
					entity.Property(e => e.Key).IsRequired().HasColumnType("varchar(20)");
					entity.Property(e => e.HashMD5).IsRequired().HasColumnType("char(32)").HasColumnName("hashMD5");
					entity.Property(e => e.HashSHA256).IsRequired().HasColumnType("char(64)").HasColumnName("hashSHA256");
				}

				//modelBuilder.Entity<ThinHashes>().HasIndex(e => e.HashMD5);
				//modelBuilder.Entity<ThinHashes>().HasIndex(e => e.HashSHA256);

				modelBuilder.Entity<ThinHashes>().ToTable("Hashes");

				//if (IsMySql)//fixes column mapping for MySql
				//	entity.Property(e => e.Key).HasColumnName("SourceKey");
			});

			modelBuilder.Entity<HashesInfo>(entity =>
			{
				entity.Property(e => e.ID).IsRequired();
				entity.Property(e => e.Alphabet).HasColumnType("varchar(100)");
				entity.Property(e => e.Count);
				entity.Property(e => e.KeyLength);
				entity.Property(e => e.IsCalculating).IsRequired();

				entity.HasKey(e => e.ID);
			});

			modelBuilder.Entity<SessionCache>(entity =>
			{
				entity.HasIndex(e => e.ExpiresAtTime)
					.HasName("Index_ExpiresAtTime");

				entity.Property(e => e.Id).HasMaxLength(449);
				entity.Property(e => e.Value).IsRequired();
			});
		}
	}
}
