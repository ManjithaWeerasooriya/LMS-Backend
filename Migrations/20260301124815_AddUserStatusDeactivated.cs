using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStatusDeactivated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE AspNetUsers
                SET Status = 1
                WHERE Status = 2
            """);

            migrationBuilder.Sql("""
                UPDATE AspNetUsers
                SET Status = 2
                WHERE Status = 3
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE AspNetUsers
                SET Status = 3
                WHERE Status = 2
            """);
        }
    }
}
