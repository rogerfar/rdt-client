using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RdtClient.Data.Migrations
{
    /// <inheritdoc />
    public partial class Migrate_Debug_Level : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Settings SET Value = 1 WHERE SettingId = 'General:LogLevel' AND Value = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Settings SET Value = 0 WHERE SettingId = 'General:LogLevel' AND Value = 1");
        }
    }
}
