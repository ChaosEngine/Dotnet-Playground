using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
#if INCLUDE_POSTGRES
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
#endif
#if INCLUDE_ORACLE
using Oracle.EntityFrameworkCore.Metadata;
#endif

namespace DotnetPlayground.Migrations
{
    public partial class DataProtectionKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
						.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
#if INCLUDE_SQLSERVER
						.Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_ORACLE
						.Annotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn)
#endif
#if INCLUDE_POSTGRES
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
#endif
                    , FriendlyName = table.Column<string>(nullable: true, maxLength: 100),
                    Xml = table.Column<string>(nullable: true, maxLength: 4000)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoogleProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Xml = table.Column<string>(nullable: true),
                    Environment = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleProtectionKeys", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "GoogleProtectionKeys");
        }
    }
}
