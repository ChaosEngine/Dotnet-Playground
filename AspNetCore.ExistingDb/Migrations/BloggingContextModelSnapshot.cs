﻿// <auto-generated />
using System;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
#if INCLUDE_ORACLE
using Oracle.EntityFrameworkCore.Metadata;
#endif

namespace AspNetCore.ExistingDb.Migrations
{
    [DbContext(typeof(BloggingContext))]
    partial class BloggingContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("AspNetCore.ExistingDb.SessionCache", b =>
            {
                b.Property<string>("Id")
                    .ValueGeneratedOnAdd()
                    .HasMaxLength(449);

                b.Property<DateTimeOffset?>("AbsoluteExpiration");

                b.Property<DateTimeOffset>("ExpiresAtTime");

                b.Property<long?>("SlidingExpirationInSeconds");

                b.Property<byte[]>("Value")
                    .IsRequired();

                b.HasKey("Id");

                b.HasIndex("ExpiresAtTime")
                    .HasDatabaseName("Index_ExpiresAtTime");

                b.ToTable("SessionCache");
            });

            modelBuilder.Entity("EFGetStarted.AspNetCore.ExistingDb.Models.Blog", b =>
            {
                b.Property<int>("BlogId")
                    .ValueGeneratedOnAdd();

                b.Property<string>("Url")
                    .IsRequired();

                b.HasKey("BlogId");

                b.ToTable("Blog");
            });

            modelBuilder.Entity("EFGetStarted.AspNetCore.ExistingDb.Models.DataProtectionKey", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("Sqlite:Autoincrement", true)
                    .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
#if INCLUDE_ORACLE
					.HasAnnotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
                    .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

                b.Property<string>("FriendlyName").HasMaxLength(100);

                b.Property<string>("Xml").HasMaxLength(4000);

                b.HasKey("Id");

                b.ToTable("DataProtectionKeys");
            });

            modelBuilder.Entity("EFGetStarted.AspNetCore.ExistingDb.Models.GoogleProtectionKey", b =>
            {
                b.Property<string>("Id");

                b.Property<string>("Environment").HasMaxLength(100);

                b.Property<string>("Json");

                b.HasKey("Id", "Environment");

                b.ToTable("GoogleProtectionKeys");
            });

            modelBuilder.Entity("EFGetStarted.AspNetCore.ExistingDb.Models.HashesInfo", b =>
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

            modelBuilder.Entity("EFGetStarted.AspNetCore.ExistingDb.Models.Post", b =>
            {
                b.Property<int>("PostId")
                    .ValueGeneratedOnAdd();

                b.Property<int>("BlogId");

                b.Property<string>("Content");

                b.Property<string>("Title");

                b.HasKey("PostId");

                b.HasIndex("BlogId");

                b.ToTable("Post");
            });

            modelBuilder.Entity("EFGetStarted.AspNetCore.ExistingDb.Models.ThinHashes", b =>
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

            modelBuilder.Entity("IdentitySample.DefaultUI.Data.ApplicationUser", b =>
            {
                b.Property<string>("Id")
                    .ValueGeneratedOnAdd();

                b.Property<int>("AccessFailedCount");

                b.Property<string>("ConcurrencyStamp")
                    .IsConcurrencyToken();

                b.Property<string>("Email")
                    .HasMaxLength(256);

                b.Property<bool>("EmailConfirmed");

                b.Property<bool>("LockoutEnabled");

                b.Property<DateTimeOffset?>("LockoutEnd");

                b.Property<string>("Name");

                b.Property<string>("NormalizedEmail")
                    .HasMaxLength(256);

                b.Property<string>("NormalizedUserName")
                    .HasMaxLength(256);

                b.Property<string>("PasswordHash");

                b.Property<string>("PhoneNumber");

                b.Property<bool>("PhoneNumberConfirmed");

                b.Property<string>("SecurityStamp");

                b.Property<bool>("TwoFactorEnabled");

                b.Property<string>("UserName")
                    .HasMaxLength(256);

                b.Property<string>("UserSettingsJSON")
                    .HasColumnName("UserSettings");

                b.HasKey("Id");

                b.HasIndex("NormalizedEmail")
                    .HasDatabaseName("EmailIndex");

                b.HasIndex("NormalizedUserName")
                    .IsUnique()
                    .HasDatabaseName("UserNameIndex");

                b.ToTable("AspNetUsers");
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
                    .HasDatabaseName("RoleNameIndex");

                b.ToTable("AspNetRoles");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd();

                b.Property<string>("ClaimType");

                b.Property<string>("ClaimValue");

                b.Property<string>("RoleId")
                    .IsRequired();

                b.HasKey("Id");

                b.HasIndex("RoleId");

                b.ToTable("AspNetRoleClaims");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd();

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

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
            {
                b.Property<string>("UserId");

                b.Property<string>("RoleId");

                b.HasKey("UserId", "RoleId");

                b.HasIndex("RoleId");

                b.ToTable("AspNetUserRoles");
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

            modelBuilder.Entity("EFGetStarted.AspNetCore.ExistingDb.Models.Post", b =>
            {
                b.HasOne("EFGetStarted.AspNetCore.ExistingDb.Models.Blog", "Blog")
                    .WithMany("Post")
                    .HasForeignKey("BlogId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Blog");
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
            {
                b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                    .WithMany()
                    .HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
            {
                b.HasOne("IdentitySample.DefaultUI.Data.ApplicationUser", null)
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
            {
                b.HasOne("IdentitySample.DefaultUI.Data.ApplicationUser", null)
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
            {
                b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                    .WithMany()
                    .HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("IdentitySample.DefaultUI.Data.ApplicationUser", null)
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
            {
                b.HasOne("IdentitySample.DefaultUI.Data.ApplicationUser", null)
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("EFGetStarted.AspNetCore.ExistingDb.Models.Blog", b =>
            {
                b.Navigation("Post");
            });
#pragma warning restore 612, 618
        }
    }
}
