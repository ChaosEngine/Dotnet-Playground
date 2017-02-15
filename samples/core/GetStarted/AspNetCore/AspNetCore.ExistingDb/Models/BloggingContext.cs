using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public partial class BloggingContext : DbContext
	{
		public virtual DbSet<Blog> Blogs { get; set; }
		public virtual DbSet<Post> Posts { get; set; }
		public virtual DbSet<ThinHashes> ThinHashes { get; set; }

		public static bool IsMySql { get; private set; }
		public static string ConnectionTypeName { get; private set; }

		public BloggingContext(DbContextOptions<BloggingContext> options)
			: base(options)
		{
			if (ConnectionTypeName == null)
			{
				ConnectionTypeName = Database.GetDbConnection().GetType().Name.ToLower();
				IsMySql = ConnectionTypeName == typeof(MySqlConnection).Name.ToLower();
			}

			//ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{	
			modelBuilder.Entity<Blog>(entity =>
			{
				entity.Property(e => e.Url).IsRequired();
			});
			modelBuilder.Entity<Blog>().ToTable("Blog");

			modelBuilder.Entity<Post>(entity =>
			{
				entity.HasOne(d => d.Blog)
					.WithMany(p => p.Post)
					.HasForeignKey(d => d.BlogId);
			});

			modelBuilder.Entity<ThinHashes>(entity =>
			{
				entity.Property(e => e.Key).IsRequired().HasColumnType("varchar(20)");
				entity.Property(e => e.HashMD5).IsRequired().HasColumnType("char(32)").HasColumnName("hashMD5");
				entity.Property(e => e.HashSHA256).IsRequired().HasColumnType("char(64)").HasColumnName("hashSHA256");

				//modelBuilder.Entity<ThinHashes>().HasIndex(e => e.HashMD5);
				//modelBuilder.Entity<ThinHashes>().HasIndex(e => e.HashSHA256);

				modelBuilder.Entity<ThinHashes>().ToTable("Hashes");

				if (IsMySql)//fixes column mapping for MySql
				{
					entity.Property(e => e.Key).HasColumnName("SourceKey");					
				}
			});
		}
	}
}
