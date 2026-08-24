using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgiVysSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantIdentityColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrimaryAppSystemId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "AppSystem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "AppSystem",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NormalizedEmail_PrimaryAppSystemId",
                table: "AspNetUsers",
                columns: new[] { "NormalizedEmail", "PrimaryAppSystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PrimaryAppSystemId",
                table: "AspNetUsers",
                column: "PrimaryAppSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSystem_CompanyId",
                table: "AppSystem",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSystem_OwnerUserId",
                table: "AppSystem",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppSystem_AspNetUsers_OwnerUserId",
                table: "AppSystem",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AppSystem_Company_CompanyId",
                table: "AppSystem",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AppSystem_PrimaryAppSystemId",
                table: "AspNetUsers",
                column: "PrimaryAppSystemId",
                principalTable: "AppSystem",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppSystem_AspNetUsers_OwnerUserId",
                table: "AppSystem");

            migrationBuilder.DropForeignKey(
                name: "FK_AppSystem_Company_CompanyId",
                table: "AppSystem");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AppSystem_PrimaryAppSystemId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NormalizedEmail_PrimaryAppSystemId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PrimaryAppSystemId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AppSystem_CompanyId",
                table: "AppSystem");

            migrationBuilder.DropIndex(
                name: "IX_AppSystem_OwnerUserId",
                table: "AppSystem");

            migrationBuilder.DropColumn(
                name: "PrimaryAppSystemId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AppSystem");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "AppSystem");
        }
    }
}
