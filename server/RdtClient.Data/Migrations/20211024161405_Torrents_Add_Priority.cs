using Microsoft.EntityFrameworkCore.Migrations;

namespace RdtClient.Data.Migrations
{
    public partial class Torrents_Add_Priority : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Torrents",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Torrents");
        }
    }
}
