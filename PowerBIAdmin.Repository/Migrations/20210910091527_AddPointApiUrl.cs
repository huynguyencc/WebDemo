using Microsoft.EntityFrameworkCore.Migrations;

namespace PowerBIAdmin.Repository.Migrations
{
    public partial class AddPointApiUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PointApiUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PointApiUrl",
                table: "Users");
        }
    }
}
