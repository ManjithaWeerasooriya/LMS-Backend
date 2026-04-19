using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyLiveSessionsToRoomsOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE LiveSessions
                SET MeetingType = 1
                """);

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "MeetingId",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "MeetingLink",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "Passcode",
                table: "LiveSessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupId",
                table: "LiveSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingId",
                table: "LiveSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingLink",
                table: "LiveSessions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Passcode",
                table: "LiveSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
