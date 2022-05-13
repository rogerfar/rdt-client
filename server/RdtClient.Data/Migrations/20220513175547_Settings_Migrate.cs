using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RdtClient.Data.Migrations
{
    public partial class Settings_Migrate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'General:LogLevel' WHERE SettingId = 'LogLevel'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'General:DownloadLimit' WHERE SettingId = 'DownloadLimit'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'General:UnpackLimit' WHERE SettingId = 'UnpackLimit'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'General:Categories' WHERE SettingId = 'Categories'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'General:RunOnTorrentCompleteFileName' WHERE SettingId = 'RunOnTorrentCompleteFileName'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'General:RunOnTorrentCompleteArguments' WHERE SettingId = 'RunOnTorrentCompleteArguments'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'DownloadClient:Client' WHERE SettingId = 'DownloadClient'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'DownloadClient:DownloadPath' WHERE SettingId = 'DownloadPath'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'DownloadClient:MappedPath' WHERE SettingId = 'MappedPath'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'DownloadClient:ChunkCount' WHERE SettingId = 'DownloadChunkCount'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'DownloadClient:MaxSpeed' WHERE SettingId = 'DownloadMaxSpeed'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'DownloadClient:Aria2cUrl' WHERE SettingId = 'Aria2cUrl'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'DownloadClient:Aria2cSecret' WHERE SettingId = 'Aria2cSecret'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'DownloadClient:TempPath' WHERE SettingId = 'TempPath'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'DownloadClient:ProxyServer' WHERE SettingId = 'ProxyServer'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:Provider' WHERE SettingId = 'Provider'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:ApiKey' WHERE SettingId = 'RealDebridApiKey'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:AutoImport' WHERE SettingId = 'ProviderAutoImport'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:AutoDelete' WHERE SettingId = 'ProviderAutoDelete'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:Timeout' WHERE SettingId = 'ProviderTimeout'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:CheckInterval' WHERE SettingId = 'ProviderCheckInterval'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:Default:Category' WHERE SettingId = 'ProviderAutoImportCategory'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:Default:OnlyDownloadAvailableFiles' WHERE SettingId = 'OnlyDownloadAvailableFiles'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:Default:MinFileSize' WHERE SettingId = 'MinFileSize'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:Default:TorrentRetryAttempts' WHERE SettingId = 'TorrentRetryAttempts'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:Default:DownloadRetryAttempts' WHERE SettingId = 'DownloadRetryAttempts'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:Default:DeleteOnError' WHERE SettingId = 'ProviderDeleteOnError'");
            migrationBuilder.Sql("UPDATE Settings SET SettingId = 'Provider:Default:TorrentLifetime' WHERE SettingId = 'TorrentLifetime'");

            migrationBuilder.Sql("UPDATE Settings SET Value = 'True' WHERE SettingId = 'Provider:AutoDelete' AND Value = '1'");
            migrationBuilder.Sql("UPDATE Settings SET Value = 'False' WHERE SettingId = 'Provider:AutoDelete' AND Value = '0'");
            migrationBuilder.Sql("UPDATE Settings SET Value = 'True' WHERE SettingId = 'Provider:AutoImport' AND Value = '1'");
            migrationBuilder.Sql("UPDATE Settings SET Value = 'False' WHERE SettingId = 'Provider:AutoImport' AND Value = '0'");
            migrationBuilder.Sql("UPDATE Settings SET Value = 'True' WHERE SettingId = 'Provider:Default:OnlyDownloadAvailableFiles' AND Value = '1'");
            migrationBuilder.Sql("UPDATE Settings SET Value = 'False' WHERE SettingId = 'Provider:Default:OnlyDownloadAvailableFiles' AND Value = '0'");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
