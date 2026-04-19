using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveSessionRecordingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcsRecordingId",
                table: "LiveSessions",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordingStartedAt",
                table: "LiveSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecordingStatus",
                table: "LiveSessions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordingStoppedAt",
                table: "LiveSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordingUrl",
                table: "LiveSessions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcsRecordingId",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "RecordingStartedAt",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "RecordingStatus",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "RecordingStoppedAt",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "RecordingUrl",
                table: "LiveSessions");
        }
    }
}
