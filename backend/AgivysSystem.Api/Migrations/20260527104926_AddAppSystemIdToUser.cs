using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgiVysSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSystemIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"SET @column_exists = (SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'AspNetUsers'
                  AND COLUMN_NAME = 'AppSystemId');
            SET @sql = IF(@column_exists = 0,
                'ALTER TABLE `AspNetUsers` ADD COLUMN `AppSystemId` int NULL',
                'SELECT 1');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"SET @index_exists = (SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'AspNetUsers'
                  AND INDEX_NAME = 'IX_AspNetUsers_AppSystemId');
            SET @sql = IF(@index_exists = 0,
                'CREATE INDEX `IX_AspNetUsers_AppSystemId` ON `AspNetUsers`(`AppSystemId`)',
                'SELECT 1');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"DELETE u FROM AspNetUsers u
                LEFT JOIN AppSystem s ON u.AppSystemId = s.Id
                WHERE u.AppSystemId IS NOT NULL AND s.Id IS NULL;");

            migrationBuilder.Sql(@"SET @fk_exists = (SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'AspNetUsers'
                  AND CONSTRAINT_NAME = 'FK_AspNetUsers_AppSystem_AppSystemId');
            SET @sql = IF(@fk_exists = 0,
                'ALTER TABLE `AspNetUsers` ADD CONSTRAINT `FK_AspNetUsers_AppSystem_AppSystemId` FOREIGN KEY (`AppSystemId`) REFERENCES `AppSystem` (`Id`) ON DELETE CASCADE',
                'SELECT 1');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AppSystem_AppSystemId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AppSystemId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AppSystemId",
                table: "AspNetUsers");
        }
    }
}
