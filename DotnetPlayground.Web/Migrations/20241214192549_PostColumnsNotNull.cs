using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotnetPlayground.Migrations
{
    /// <inheritdoc />
    public partial class PostColumnsNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Post",
                // type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                // oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Post",
                // type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                // oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Post",
                // type: "TEXT",
                nullable: true,
                // oldType: "TEXT",
                oldClrType: typeof(string)
                );

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Post",
                // type: "TEXT",
                nullable: true,
                // oldType: "TEXT",
                oldClrType: typeof(string)
                );
        }
    }
}
