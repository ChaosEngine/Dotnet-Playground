using AspNetCore.ExistingDb.Models;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AspNetCore.ExistingDb.Migrations
{
	public partial class UserSettings : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
				name: "UserSettings",
				table: "AspNetUsers",
				type: BloggingContext.JsonColumnTypeFromProvider(this.ActiveProvider),
				nullable: true);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "UserSettings",
				table: "AspNetUsers");
		}
	}
}
