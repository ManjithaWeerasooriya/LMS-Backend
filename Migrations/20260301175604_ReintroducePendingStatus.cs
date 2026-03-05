using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class ReintroducePendingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE AspNetUsers
                SET Status =
                    CASE
                        WHEN Status = 2 THEN 3
                        WHEN Status = 3 THEN 2
                        ELSE Status
                    END;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE AspNetUsers
                SET Status =
                    CASE
                        WHEN Status = 3 THEN 2
                        WHEN Status = 2 THEN 3
                        ELSE Status
                    END;
            """);
        }
    }
}
