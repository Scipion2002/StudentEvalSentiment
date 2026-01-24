using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentEvalSentiment.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedLikertSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedLikertSummaries",
                columns: table => new
                {
                    ProcessedLikertSummaryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InstructorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CourseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    LikertAvg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LikertCountUsed = table.Column<int>(type: "int", nullable: false),
                    LabelDerived = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Dim1Avg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Dim2Avg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Dim3Avg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Dim4Avg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Dim5Avg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedLikertSummaries", x => x.ProcessedLikertSummaryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedLikertSummaries_ImportBatchId_TargetType_InstructorName_CourseNumber",
                table: "ProcessedLikertSummaries",
                columns: new[] { "ImportBatchId", "TargetType", "InstructorName", "CourseNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedLikertSummaries");
        }
    }
}
