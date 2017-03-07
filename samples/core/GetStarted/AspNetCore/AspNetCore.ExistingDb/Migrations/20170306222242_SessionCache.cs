using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AspNetCore.ExistingDb.Migrations
{
    public partial class SessionCache : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionCache",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 449, nullable: false),
                    AbsoluteExpiration = table.Column<DateTimeOffset>(nullable: true),
                    ExpiresAtTime = table.Column<DateTimeOffset>(nullable: false),
                    SlidingExpirationInSeconds = table.Column<long>(nullable: true),
                    Value = table.Column<byte[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "Index_ExpiresAtTime",
                table: "SessionCache",
                column: "ExpiresAtTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionCache");
        }
    }
}
