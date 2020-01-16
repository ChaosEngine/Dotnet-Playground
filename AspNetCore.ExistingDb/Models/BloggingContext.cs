using AspNetCore.ExistingDb;
using IdentitySample.DefaultUI.Data;
using InkBall.Module.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public class BloggingContextFactory : ContextFactory, IDesignTimeDbContextFactory<BloggingContext>
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
		public BloggingContext CreateDbContext(string[] args)
		{
			var configuration = GetConfiguration(args);

			var optionsBuilder = new DbContextOptionsBuilder<BloggingContext>();

			ConfigureDBKind(optionsBuilder, configuration, null);

			return new BloggingContext(optionsBuilder.Options);
		}
	}

	public partial class BloggingContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext, IGoogleKeyContext
	{
		public virtual DbSet<Blog> Blogs { get; set; }
		public virtual DbSet<Post> Posts { get; set; }
		public virtual DbSet<ThinHashes> ThinHashes { get; set; }
		public virtual DbSet<HashesInfo> HashesInfo { get; set; }
		public virtual DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
		public virtual DbSet<GoogleProtectionKey> GoogleProtectionKeys { get; set; }

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
					case "Oracle.EntityFrameworkCore":
						return "oracleconnection";
					default:
						throw new NotSupportedException($"Bad DBKind name {Database.ProviderName}");
				}
			}
		}

		internal static string JsonColumnTypeFromProvider(string activeProvider)
		{
			switch (activeProvider)
			{
				case "Microsoft.EntityFrameworkCore.SqlServer":
					return "nvarchar(1000)";

				case "Pomelo.EntityFrameworkCore.MySql":
					return "json";

				case "Microsoft.EntityFrameworkCore.Sqlite":
					return "TEXT";

				case "Npgsql.EntityFrameworkCore.PostgreSQL":
					return "jsonb";

				case "Oracle.EntityFrameworkCore":
					return "CLOB";

				default:
					throw new NotSupportedException($"Bad DBKind name {activeProvider}");
			}
		}

		public BloggingContext(DbContextOptions<BloggingContext> options)
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			// Customize the ASP.NET Identity model and override the defaults if needed.
			// For example, you can rename the ASP.NET Identity table names and more.
			// Add your customizations after calling base.OnModelCreating(builder);

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

			modelBuilder.Entity<DataProtectionKey>(entity =>
			{
				entity.HasKey(e => e.Id);

				entity.Property(e => e.Id).ValueGeneratedOnAdd();

				entity.Property(e => e.Xml).HasMaxLength(4000);

				entity.ToTable("DataProtectionKeys");
			});

			modelBuilder.Entity<GoogleProtectionKey>(entity =>
			{
				entity.HasKey(e => new { e.Id, e.Environment });

				entity.Property(e => e.Environment)
					.HasConversion(new EnumToNumberConverter<EnvEnum, int>());

				entity.ToTable("GoogleProtectionKeys");
			});

			modelBuilder.Entity<ApplicationUser>(entity =>
			{
				// This Converter will perform the conversion to and from Json to the desired type
				entity.Property(e => e.UserSettingsJSON)
				// 	.HasConversion(
				// 	v => JsonConvert.SerializeObject(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
				// 	v => JsonConvert.DeserializeObject<ApplicationUserSettings>(v, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
				// )
					.HasColumnName("UserSettings")
					;
			});
		}
	}
}
