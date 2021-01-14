using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using DotnetPlayground.Models;
#if INCLUDE_ORACLE
using Oracle.EntityFrameworkCore.Metadata;
#endif

namespace DotnetPlayground.Migrations
{
    [DbContext(typeof(BloggingContext))]
    [Migration("20170218162130_HashesInfo")]
    partial class HashesInfo
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752")
#if INCLUDE_ORACLE
				.HasAnnotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("DotnetPlayground.Models.Blog", b =>
                {
                    b.Property<int>("BlogId")
                        .ValueGeneratedOnAdd()
#if INCLUDE_ORACLE
						.HasAnnotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Url")
                        .IsRequired();

                    b.HasKey("BlogId");

                    b.ToTable("Blog");
                });

            modelBuilder.Entity("DotnetPlayground.Models.HashesInfo", b =>
                {
                    b.Property<int>("ID");

                    b.Property<string>("Alphabet")
                        .HasColumnType("varchar(100)");

                    b.Property<int>("Count");

                    b.Property<bool>("IsCalculating");

                    b.Property<int>("KeyLength");

                    b.HasKey("ID");

                    b.ToTable("HashesInfo");
                });

            modelBuilder.Entity("DotnetPlayground.Models.Post", b =>
                {
                    b.Property<int>("PostId")
                        .ValueGeneratedOnAdd()
#if INCLUDE_ORACLE
						.HasAnnotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("BlogId");

                    b.Property<string>("Content");

                    b.Property<string>("Title");

                    b.HasKey("PostId");

                    b.HasIndex("BlogId");

                    b.ToTable("Post");
                });

            modelBuilder.Entity("DotnetPlayground.Models.ThinHashes", b =>
                {
                    b.Property<string>("Key")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("varchar(20)");

                    b.Property<string>("HashMD5")
                        .IsRequired()
                        .HasColumnName("hashMD5")
                        .HasColumnType("char(32)");

                    b.Property<string>("HashSHA256")
                        .IsRequired()
                        .HasColumnName("hashSHA256")
                        .HasColumnType("char(64)");

                    b.HasKey("Key");

                    b.ToTable("Hashes");
                });

            modelBuilder.Entity("DotnetPlayground.Models.Post", b =>
                {
                    b.HasOne("DotnetPlayground.Models.Blog", "Blog")
                        .WithMany("Post")
                        .HasForeignKey("BlogId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
