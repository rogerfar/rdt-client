using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RdtClient.Data.Migrations
{
    public partial class Settings_Migrate_InternalDownloader : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Settings SET Value = 0 WHERE SettingId = 'DownloadClient:Client' AND Value = 1");
            migrationBuilder.Sql("UPDATE Settings SET Value = 1 WHERE SettingId = 'DownloadClient:Client' AND Value = 2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
