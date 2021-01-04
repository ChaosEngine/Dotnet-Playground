using DotnetPlayground.Models;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DotnetPlayground.Migrations
{
    public partial class GoogleProtection_Refactoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Xml",
                table: "GoogleProtectionKeys",
                newName: "Json");

            migrationBuilder.AlterColumn<string>(
                name: "Environment",
                table: "GoogleProtectionKeys",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Json",
                table: "GoogleProtectionKeys",
                type: BloggingContext.JsonColumnTypeFromProvider(this.ActiveProvider),
                nullable: true,
                oldType: "varchar(100)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Json",
                table: "GoogleProtectionKeys",
                type: "varchar(100)",
                nullable: true,
                oldType: BloggingContext.JsonColumnTypeFromProvider(this.ActiveProvider));

            migrationBuilder.RenameColumn(
                name: "Json",
                table: "GoogleProtectionKeys",
                newName: "Xml");

            migrationBuilder.AlterColumn<int>(
                name: "Environment",
                table: "GoogleProtectionKeys",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);
        }
    }
}
