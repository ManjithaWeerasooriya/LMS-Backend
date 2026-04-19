using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleMeetingLocator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AcsRoomId",
                table: "LiveSessions",
                newName: "RoomId");

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

            migrationBuilder.AddColumn<int>(
                name: "MeetingType",
                table: "LiveSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Passcode",
                table: "LiveSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE LiveSessions
                SET RoomId = LTRIM(RTRIM(SUBSTRING(AcsCallLocator, LEN('room:') + 1, LEN(AcsCallLocator))))
                WHERE (RoomId IS NULL OR LTRIM(RTRIM(RoomId)) = '')
                  AND AcsCallLocator IS NOT NULL
                  AND LTRIM(RTRIM(AcsCallLocator)) LIKE 'room:%';
                """);

            migrationBuilder.Sql("""
                UPDATE LiveSessions
                SET GroupId = LTRIM(RTRIM(SUBSTRING(AcsCallLocator, LEN('group:') + 1, LEN(AcsCallLocator))))
                WHERE AcsCallLocator IS NOT NULL
                  AND LTRIM(RTRIM(AcsCallLocator)) LIKE 'group:%';
                """);

            migrationBuilder.Sql("""
                UPDATE LiveSessions
                SET MeetingLink = LTRIM(RTRIM(AcsCallLocator))
                WHERE AcsCallLocator IS NOT NULL
                  AND (
                    LTRIM(RTRIM(AcsCallLocator)) LIKE 'https://%'
                    OR LTRIM(RTRIM(AcsCallLocator)) LIKE 'http://%'
                  );
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM LiveSessions
                    WHERE AcsCallLocator IS NOT NULL
                      AND LTRIM(RTRIM(AcsCallLocator)) <> ''
                      AND NOT (
                        LTRIM(RTRIM(AcsCallLocator)) LIKE 'room:%'
                        OR LTRIM(RTRIM(AcsCallLocator)) LIKE 'group:%'
                        OR LTRIM(RTRIM(AcsCallLocator)) LIKE 'https://%'
                        OR LTRIM(RTRIM(AcsCallLocator)) LIKE 'http://%'
                      )
                )
                BEGIN
                    THROW 50000, 'Unsupported legacy live-session locator detected. Clean the data before applying EnforceSingleMeetingLocator.', 1;
                END
                """);

            migrationBuilder.Sql("""
                UPDATE LiveSessions
                SET MeetingType = CASE
                    WHEN RoomId IS NOT NULL AND LTRIM(RTRIM(RoomId)) <> '' THEN 1
                    WHEN GroupId IS NOT NULL AND LTRIM(RTRIM(GroupId)) <> '' THEN 2
                    WHEN MeetingLink IS NOT NULL AND LTRIM(RTRIM(MeetingLink)) <> '' THEN 3
                    ELSE NULL
                END;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM LiveSessions
                    WHERE
                        (CASE WHEN RoomId IS NOT NULL AND LTRIM(RTRIM(RoomId)) <> '' THEN 1 ELSE 0 END) +
                        (CASE WHEN GroupId IS NOT NULL AND LTRIM(RTRIM(GroupId)) <> '' THEN 1 ELSE 0 END) +
                        (CASE WHEN MeetingLink IS NOT NULL AND LTRIM(RTRIM(MeetingLink)) <> '' THEN 1 ELSE 0 END) +
                        (CASE WHEN MeetingId IS NOT NULL AND LTRIM(RTRIM(MeetingId)) <> '' THEN 1 ELSE 0 END)
                        <> 1
                        OR MeetingType IS NULL
                )
                BEGIN
                    THROW 50001, 'LiveSessions must contain exactly one migrated meeting locator.', 1;
                END
                """);

            migrationBuilder.AlterColumn<int>(
                name: "MeetingType",
                table: "LiveSessions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "AcsCallLocator",
                table: "LiveSessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcsCallLocator",
                table: "LiveSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE LiveSessions
                SET AcsCallLocator = CASE
                    WHEN GroupId IS NOT NULL AND LTRIM(RTRIM(GroupId)) <> '' THEN CONCAT('group:', GroupId)
                    WHEN MeetingLink IS NOT NULL AND LTRIM(RTRIM(MeetingLink)) <> '' THEN MeetingLink
                    WHEN MeetingId IS NOT NULL AND LTRIM(RTRIM(MeetingId)) <> '' THEN MeetingId
                    ELSE NULL
                END;
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
                name: "MeetingType",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "Passcode",
                table: "LiveSessions");

            migrationBuilder.RenameColumn(
                name: "RoomId",
                table: "LiveSessions",
                newName: "AcsRoomId");
        }
    }
}
