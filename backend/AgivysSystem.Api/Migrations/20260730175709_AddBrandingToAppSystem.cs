using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgiVysSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingToAppSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackgroundColor",
                table: "AppSystem",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Domain",
                table: "AppSystem",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TextColor",
                table: "AppSystem",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackgroundColor",
                table: "AppSystem");

            migrationBuilder.DropColumn(
                name: "Domain",
                table: "AppSystem");

            migrationBuilder.DropColumn(
                name: "TextColor",
                table: "AppSystem");
        }
    }
}
