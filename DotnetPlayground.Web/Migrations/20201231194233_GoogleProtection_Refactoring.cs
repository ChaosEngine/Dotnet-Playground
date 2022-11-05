using DotnetPlayground.Models;
using Microsoft.EntityFrameworkCore;
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


			//Postrgres's JSONB data conversion needs `column USING "column"::jsonb` as a conversion way(?)
			//description of similiar bug: https://github.com/npgsql/efcore.pg/issues/144
            // all in all: need to cover to postgresql special case, even when there is no data(!)
			string json_type = BloggingContext.JsonColumnTypeFromProvider(this.ActiveProvider);
			if (migrationBuilder.IsNpgsql())
			{
				migrationBuilder.AlterColumn<string>(
					name: "Json",
					table: "GoogleProtectionKeys",
					type: @$"{json_type} USING ""Json""::{json_type}",
					nullable: true,
					oldType: "varchar(100)");
			}
            else
			{
				migrationBuilder.AlterColumn<string>(
                    name: "Json",
                    table: "GoogleProtectionKeys",
                    type: json_type,
					nullable: true,
                    oldType: "varchar(100)");
            }
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
