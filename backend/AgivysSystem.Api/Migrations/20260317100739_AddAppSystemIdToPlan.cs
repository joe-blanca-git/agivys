using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgiVysSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSystemIdToPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppSystem_Plan_PlanId",
                table: "AppSystem");

            migrationBuilder.DropIndex(
                name: "IX_AppSystem_PlanId",
                table: "AppSystem");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "AppSystem");

            migrationBuilder.AddColumn<int>(
                name: "AppSystemId",
                table: "Plan",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Plan",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PlanFunctionalities",
                columns: table => new
                {
                    FunctionalitiesId = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanFunctionalities", x => new { x.FunctionalitiesId, x.PlanId });
                    table.ForeignKey(
                        name: "FK_PlanFunctionalities_Functionalities_FunctionalitiesId",
                        column: x => x.FunctionalitiesId,
                        principalTable: "Functionalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanFunctionalities_Plan_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Plan_AppSystemId",
                table: "Plan",
                column: "AppSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanFunctionalities_PlanId",
                table: "PlanFunctionalities",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Plan_AppSystem_AppSystemId",
                table: "Plan",
                column: "AppSystemId",
                principalTable: "AppSystem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plan_AppSystem_AppSystemId",
                table: "Plan");

            migrationBuilder.DropTable(
                name: "PlanFunctionalities");

            migrationBuilder.DropIndex(
                name: "IX_Plan_AppSystemId",
                table: "Plan");

            migrationBuilder.DropColumn(
                name: "AppSystemId",
                table: "Plan");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Plan");

            migrationBuilder.AddColumn<int>(
                name: "PlanId",
                table: "AppSystem",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSystem_PlanId",
                table: "AppSystem",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppSystem_Plan_PlanId",
                table: "AppSystem",
                column: "PlanId",
                principalTable: "Plan",
                principalColumn: "Id");
        }
    }
}
