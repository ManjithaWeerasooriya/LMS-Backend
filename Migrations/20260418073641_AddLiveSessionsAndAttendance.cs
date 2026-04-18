using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveSessionsAndAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiveSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RecordingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PlaybackEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AcsRoomId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AcsCallLocator = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChatThreadId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByTeacherId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveSessions_AspNetUsers_CreatedByTeacherId",
                        column: x => x.CreatedByTeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LiveSessions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiveSessionAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JoinTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaveTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    AttendanceStatus = table.Column<int>(type: "int", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveSessionAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveSessionAttendances_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LiveSessionAttendances_LiveSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "LiveSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessionAttendances_LastSeenAt",
                table: "LiveSessionAttendances",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessionAttendances_SessionId_StudentId",
                table: "LiveSessionAttendances",
                columns: new[] { "SessionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessionAttendances_StudentId",
                table: "LiveSessionAttendances",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_CourseId_StartTime",
                table: "LiveSessions",
                columns: new[] { "CourseId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveSessions_CreatedByTeacherId_StartTime",
                table: "LiveSessions",
                columns: new[] { "CreatedByTeacherId", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveSessionAttendances");

            migrationBuilder.DropTable(
                name: "LiveSessions");
        }
    }
}
