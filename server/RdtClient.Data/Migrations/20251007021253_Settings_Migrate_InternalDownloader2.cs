using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RdtClient.Data.Migrations
{
    /// <inheritdoc />
    public partial class Settings_Migrate_InternalDownloader2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Settings SET Value = 0 WHERE SettingId = 'DownloadClient:Client' AND Value = 1");
            migrationBuilder.Sql("UPDATE Settings SET Value = 1 WHERE SettingId = 'DownloadClient:Client' AND Value = 2");
            migrationBuilder.Sql("UPDATE Settings SET Value = 2 WHERE SettingId = 'DownloadClient:Client' AND Value = 3");
            migrationBuilder.Sql("UPDATE Settings SET Value = 3 WHERE SettingId = 'DownloadClient:Client' AND Value = 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
