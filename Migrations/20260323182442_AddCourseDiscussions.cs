using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseDiscussions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseDiscussionMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ParentMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseDiscussionMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseDiscussionMessages_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseDiscussionMessages_CourseDiscussionMessages_ParentMessageId",
                        column: x => x.ParentMessageId,
                        principalTable: "CourseDiscussionMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseDiscussionMessages_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseDiscussionMessages_CourseId",
                table: "CourseDiscussionMessages",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDiscussionMessages_ParentMessageId",
                table: "CourseDiscussionMessages",
                column: "ParentMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDiscussionMessages_StudentId",
                table: "CourseDiscussionMessages",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseDiscussionMessages");
        }
    }
}
