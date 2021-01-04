using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DotnetPlayground.Migrations
{
    public partial class HashesInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HashesInfo",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false),
                    Alphabet = table.Column<string>(type: "varchar(100)", nullable: true),
                    Count = table.Column<int>(nullable: false),
                    IsCalculating = table.Column<bool>(nullable: false),
                    KeyLength = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashesInfo", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HashesInfo");
        }
    }
}
