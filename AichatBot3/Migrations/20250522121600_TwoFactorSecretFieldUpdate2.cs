using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AichatBot3.Migrations
{
    public partial class TwoFactorSecretFieldUpdate2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Preferred2FAMethod",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Preferred2FAMethod",
                table: "AspNetUsers");
        }
    }
}
