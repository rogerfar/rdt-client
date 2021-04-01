using Microsoft.EntityFrameworkCore.Migrations;

namespace RdtClient.Data.Migrations
{
    public partial class Downloads_Add_RetryCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "Downloads",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "Downloads");
        }
    }
}
