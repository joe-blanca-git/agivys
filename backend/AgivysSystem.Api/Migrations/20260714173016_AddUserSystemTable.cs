using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgiVysSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSystemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AppSystem_AppSystemId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Payments");



            migrationBuilder.DropColumn(
                name: "AsaasCustomerId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "Route",
                table: "Menus",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserAccessMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MenuId = table.Column<int>(type: "int", nullable: false),
                    AppSystemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccessMaps_AppSystem_AppSystemId",
                        column: x => x.AppSystemId,
                        principalTable: "AppSystem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAccessMaps_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAccessMaps_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserSystems",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AppSystemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSystems", x => new { x.UserId, x.AppSystemId });
                    table.ForeignKey(
                        name: "FK_UserSystems_AppSystem_AppSystemId",
                        column: x => x.AppSystemId,
                        principalTable: "AppSystem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSystems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessMaps_AppSystemId",
                table: "UserAccessMaps",
                column: "AppSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessMaps_MenuId",
                table: "UserAccessMaps",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessMaps_UserId",
                table: "UserAccessMaps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSystems_AppSystemId",
                table: "UserSystems",
                column: "AppSystemId");

            // --- INJEÇÃO MANUAL DE PRESERVAÇÃO DE DADOS ---
            migrationBuilder.Sql("INSERT INTO UserSystems (UserId, AppSystemId, CreatedAt) SELECT Id, AppSystemId, UTC_TIMESTAMP() FROM AspNetUsers WHERE AppSystemId IS NOT NULL AND AppSystemId > 0");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AppSystemId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AppSystemId",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAccessMaps");

            migrationBuilder.DropTable(
                name: "UserSystems");

            migrationBuilder.DropColumn(
                name: "Route",
                table: "Menus");

            migrationBuilder.AddColumn<int>(
                name: "AppSystemId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AsaasCustomerId",
                table: "AspNetUsers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AsaasId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AsaasSubscriptionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BillingType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NetValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AppSystemId",
                table: "AspNetUsers",
                column: "AppSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AppSystem_AppSystemId",
                table: "AspNetUsers",
                column: "AppSystemId",
                principalTable: "AppSystem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
