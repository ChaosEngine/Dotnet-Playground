using System;
using InkBall.Module.Model;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#if INCLUDE_POSTGRES
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
#endif
#if INCLUDE_ORACLE
using Oracle.EntityFrameworkCore.Metadata;
#endif

#nullable disable

namespace DotnetPlayground.Migrations
{
    /// <inheritdoc />
    public partial class ErrorLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorLog",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
#if INCLUDE_MYSQL
						.Annotation("MySql:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_SQLSERVER
						.Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_ORACLE
						.Annotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_POSTGRES
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
#endif
                    ,
                    HttpStatus = table.Column<int>(nullable: true),
                    Url = table.Column<string>(maxLength: 1000, nullable: true),
                    Message = table.Column<string>(maxLength: 4000, nullable: true),
                    Line = table.Column<int>(nullable: true),
                    Column = table.Column<int>(nullable: true),
                    Created = table.Column<DateTime>(
                        type: GamesContext.TimeStampColumnTypeFromProvider(ActiveProvider,
                        "TEXT", "datetime", "timestamp without time zone", "TIMESTAMP(7)", "datetime2"),
                        nullable: false,
						defaultValueSql: GamesContext.TimeStampDefaultValueFromProvider(ActiveProvider,
							"datetime('now','localtime')",
							"CURRENT_TIMESTAMP",
							"CURRENT_TIMESTAMP",
							"CURRENT_TIMESTAMP",
							"GETDATE()")
                        )
                },
                constraints: table =>
                {
                    if (ActiveProvider.ToLowerInvariant() == "microsoft.entityframeworkcore.sqlite" || ActiveProvider.ToLowerInvariant() == "sqlite")
                        table.PrimaryKey("PK_ErrorLog", x => x.Id);
                    else
                        table.PrimaryKey("PK_ErrorLog", x => new { x.Id, x.Created });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorLog");
        }
    }
}
