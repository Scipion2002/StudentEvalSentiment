using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentEvalSentiment.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedCommentsStaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedComments",
                columns: table => new
                {
                    ProcessedCommentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InstructorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CourseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TextClean = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedComments", x => x.ProcessedCommentId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedComments_CourseNumber",
                table: "ProcessedComments",
                column: "CourseNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedComments_ImportBatchId",
                table: "ProcessedComments",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedComments_InstructorName",
                table: "ProcessedComments",
                column: "InstructorName");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedComments_TargetType",
                table: "ProcessedComments",
                column: "TargetType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedComments");
        }
    }
}
