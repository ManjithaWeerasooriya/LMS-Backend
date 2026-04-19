using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAcsIdentityMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcsIdentityId",
                table: "AspNetUsers",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcsIdentityId",
                table: "AspNetUsers");
        }
    }
}
