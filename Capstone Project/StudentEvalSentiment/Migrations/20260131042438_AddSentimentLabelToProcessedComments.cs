using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentEvalSentiment.Migrations
{
    /// <inheritdoc />
    public partial class AddSentimentLabelToProcessedComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SentimentLabel",
                table: "ProcessedComments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedComments_ImportBatchId_TargetType_SentimentLabel",
                table: "ProcessedComments",
                columns: new[] { "ImportBatchId", "TargetType", "SentimentLabel" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedComments_ImportBatchId_TargetType_SentimentLabel",
                table: "ProcessedComments");

            migrationBuilder.DropColumn(
                name: "SentimentLabel",
                table: "ProcessedComments");
        }
    }
}
