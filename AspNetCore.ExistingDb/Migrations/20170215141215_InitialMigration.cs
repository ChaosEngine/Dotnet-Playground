using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
#if INCLUDE_ORACLE
using Oracle.EntityFrameworkCore.Metadata;
#endif

namespace AspNetCore.ExistingDb.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Blog",
                columns: table => new
                {
                    BlogId = table.Column<int>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true)
						.Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
#if INCLUDE_ORACLE
						.Annotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
					Url = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog", x => x.BlogId);
                });

            migrationBuilder.CreateTable(
                name: "Hashes",
                columns: table => new
                {
					Key = table.Column<string>(type: "varchar(20)", nullable: false),
					hashMD5 = table.Column<string>(type: "char(32)", nullable: false),
					hashSHA256 = table.Column<string>(type: "char(64)", nullable: false)
				},
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hashes", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Post",
                columns: table => new
                {
                    PostId = table.Column<int>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true)
#if INCLUDE_ORACLE
						.Annotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
						.Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    BlogId = table.Column<int>(nullable: false),
                    Content = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Post", x => x.PostId);
                    table.ForeignKey(
                        name: "FK_Post_Blog_BlogId",
                        column: x => x.BlogId,
                        principalTable: "Blog",
                        principalColumn: "BlogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Post_BlogId",
                table: "Post",
                column: "BlogId");

			//for SqlServer create user,login and access rights
			if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
				migrationBuilder.Sql(@"IF NOT EXISTS(SELECT principal_id FROM sys.server_principals WHERE name = 'test')  CREATE LOGIN test WITH PASSWORD = 'P@ssw0rd123'
CREATE USER [test] FOR LOGIN [test]
ALTER USER [test] WITH DEFAULT_SCHEMA=[dbo]
use [test];
ALTER ROLE [db_owner] ADD MEMBER [test]
");
		}

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Post");

            migrationBuilder.DropTable(
                name: "Hashes");

            migrationBuilder.DropTable(
                name: "Blog");
        }
    }
}
