using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentEvalSentiment.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionKeyAndQuestionMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuestionKey",
                table: "ProcessedComments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProcessedQuestionMaps",
                columns: table => new
                {
                    QuestionKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QuestionHeader = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    FirstSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedQuestionMaps", x => x.QuestionKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedComments_ImportBatchId_QuestionKey",
                table: "ProcessedComments",
                columns: new[] { "ImportBatchId", "QuestionKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedQuestionMaps");

            migrationBuilder.DropIndex(
                name: "IX_ProcessedComments_ImportBatchId_QuestionKey",
                table: "ProcessedComments");

            migrationBuilder.DropColumn(
                name: "QuestionKey",
                table: "ProcessedComments");
        }
    }
}
