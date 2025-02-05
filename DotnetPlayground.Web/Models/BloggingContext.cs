using DotnetPlayground;
using DotnetPlayground.Models;
using InkBall.Module.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using System;
#if INCLUDE_POSTGRES
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
#endif
#if INCLUDE_ORACLE
using Oracle.EntityFrameworkCore.Metadata;
#endif

namespace DotnetPlayground.Models
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

			InkBall.Module.ContextSnapshotHelper.DBKind = configuration.GetValue<string>("DBKind");
            //Console.WriteLine($"DBKind = {configuration.GetValue<string>("DBKind")}");

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
		public virtual DbSet<ErrorLog> ErrorLogs { get; set; }

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

		internal static string JsonColumnTypeFromProvider(string activeProvider) =>
			GamesContext.JsonColumnTypeFromProvider(activeProvider,
				"TEXT", "json", "jsonb", "VARCHAR2(4000)", "NVARCHAR(1000)"
			);

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
				entity.Property(e => e.BlogId)
					.ValueGeneratedOnAdd()
					.HasAnnotation("Sqlite:Autoincrement", true)
#if INCLUDE_MYSQL
					.HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_SQLSERVER
					.HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_ORACLE
					.HasAnnotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_POSTGRES
					.HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
#endif
					;

				entity.Property(e => e.Url).IsRequired();
				entity.ToTable("Blog");
			});

			modelBuilder.Entity<Post>(entity =>
			{
				entity.HasOne(d => d.Blog)
					.WithMany(p => p.Post)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasForeignKey(d => d.BlogId);

				entity.Property(e => e.PostId)
					.ValueGeneratedOnAdd()
					.HasAnnotation("Sqlite:Autoincrement", true)
#if INCLUDE_MYSQL
					.HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_SQLSERVER
					.HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_ORACLE
					.HasAnnotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_POSTGRES
					.HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
#endif
					;

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

				modelBuilder.Entity<ThinHashes>().ToTable("Hashes");
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
					.HasDatabaseName("Index_ExpiresAtTime");

				entity.Property(e => e.Id).HasMaxLength(449);
				entity.Property(e => e.Value).IsRequired();
			});

			modelBuilder.Entity<DataProtectionKey>(entity =>
			{
				entity.HasKey(e => e.Id);

				entity.Property(e => e.Id).ValueGeneratedOnAdd();

				entity.Property(e => e.FriendlyName).HasMaxLength(100);

				entity.Property(e => e.Xml).HasMaxLength(4000);

				entity.ToTable("DataProtectionKeys");
			});

			modelBuilder.Entity<GoogleProtectionKey>(entity =>
			{
				entity.HasKey(e => new { e.Id, e.Environment });

				entity.Property(e => e.Environment)
					.HasMaxLength(100);

				entity.Property(e => e.Json)
					.HasColumnType(BloggingContext.JsonColumnTypeFromProvider(Database.ProviderName));

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
					.HasColumnType(BloggingContext.JsonColumnTypeFromProvider(Database.ProviderName));

				entity.HasIndex("NormalizedUserName")
                    .IsUnique()
                    .HasDatabaseName("UserNameIndex")
                    .HasFilter(GamesContext.HasIndexFilterFromProvider(Database.ProviderName,
						null, null, null,null, "[NormalizedUserName] IS NOT NULL")
					);
			});

			modelBuilder.Entity<ErrorLog>(entity =>
			{
				entity.Property(e => e.Id)
					.ValueGeneratedOnAdd()
					.HasAnnotation("Sqlite:Autoincrement", true)
#if INCLUDE_MYSQL
					.HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_SQLSERVER
					.HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_ORACLE
					.HasAnnotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_POSTGRES
					.HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
#endif
					;
				entity.HasKey(e => e.Id);


				entity.Property(e => e.HttpStatus)
					.HasColumnName("HttpStatus");

				entity.Property(e => e.Url)
					.HasColumnName("Url")
					.HasMaxLength(1000);

				entity.Property(e => e.Message)
					.HasColumnName("Message")
					.HasMaxLength(4000);
					
				entity.Property(e => e.Line)
					.HasColumnName("Line");
					
				entity.Property(e => e.Column)
					.HasColumnName("Column");

				entity.Property(e => e.Created)
					.ValueGeneratedOnAddOrUpdate()
					.HasColumnType(GamesContext.TimeStampColumnTypeFromProvider(Database.ProviderName,
						"TEXT", "timestamp", "timestamp without time zone", "TIMESTAMP(7)", "datetime2"
					))
					.HasDefaultValueSql(GamesContext.TimeStampDefaultValueFromProvider(Database.ProviderName,
						"datetime('now','localtime')",
						"CURRENT_TIMESTAMP",
						"CURRENT_TIMESTAMP",
						"CURRENT_TIMESTAMP",
						"GETDATE()"));

				entity.ToTable("ErrorLog");
			});

			modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
            {
                b.Property<string>("UserId");

                b.Property<string>("LoginProvider")
                    .HasMaxLength(128);

                b.Property<string>("Name")
                    .HasMaxLength(128);

                b.Property<string>("Value");

                b.HasKey("UserId", "LoginProvider", "Name");

                b.ToTable("AspNetUserTokens");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
					.HasAnnotation("Sqlite:Autoincrement", true)
#if INCLUDE_MYSQL
					.HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_SQLSERVER
					.HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_ORACLE
					.HasAnnotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_POSTGRES
					.HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
#endif
					;

                b.Property<string>("ClaimType");

                b.Property<string>("ClaimValue");

                b.Property<string>("UserId")
                    .IsRequired();

                b.HasKey("Id");

                b.HasIndex("UserId");

                b.ToTable("AspNetUserClaims");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
            {
                b.Property<string>("LoginProvider")
                    .HasMaxLength(128);

                b.Property<string>("ProviderKey")
                    .HasMaxLength(128);

                b.Property<string>("ProviderDisplayName");

                b.Property<string>("UserId")
                    .IsRequired();

                b.HasKey("LoginProvider", "ProviderKey");

                b.HasIndex("UserId");

                b.ToTable("AspNetUserLogins");
            });

			modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
					.HasAnnotation("Sqlite:Autoincrement", true)
#if INCLUDE_MYSQL
					.HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_SQLSERVER
					.HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_ORACLE
					.HasAnnotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_POSTGRES
					.HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
#endif
					;

                b.Property<string>("ClaimType");

                b.Property<string>("ClaimValue");

                b.Property<string>("RoleId")
                    .IsRequired();

                b.HasKey("Id");

                b.HasIndex("RoleId");

                b.ToTable("AspNetRoleClaims");
            });

			modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
            {
                b.Property<string>("Id")
                    .ValueGeneratedOnAdd();

                b.Property<string>("ConcurrencyStamp")
                    .IsConcurrencyToken();

                b.Property<string>("Name")
                    .HasMaxLength(256);

                b.Property<string>("NormalizedName")
                    .HasMaxLength(256);

                b.HasKey("Id");

                b.HasIndex("NormalizedName")
                    .IsUnique()
                    .HasDatabaseName("RoleNameIndex")
                    .HasFilter(GamesContext.HasIndexFilterFromProvider(Database.ProviderName,
						null, null, null,null, "[NormalizedName] IS NOT NULL")
					);

                b.ToTable("AspNetRoles");
            });
		}
	}
}
