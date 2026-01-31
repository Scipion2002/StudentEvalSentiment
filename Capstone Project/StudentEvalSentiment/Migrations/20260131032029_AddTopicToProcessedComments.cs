using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentEvalSentiment.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicToProcessedComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TopicAssignedUtc",
                table: "ProcessedComments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TopicClusterId",
                table: "ProcessedComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopicModel",
                table: "ProcessedComments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedComments_ImportBatchId_TargetType_TopicClusterId",
                table: "ProcessedComments",
                columns: new[] { "ImportBatchId", "TargetType", "TopicClusterId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedComments_ImportBatchId_TargetType_TopicClusterId",
                table: "ProcessedComments");

            migrationBuilder.DropColumn(
                name: "TopicAssignedUtc",
                table: "ProcessedComments");

            migrationBuilder.DropColumn(
                name: "TopicClusterId",
                table: "ProcessedComments");

            migrationBuilder.DropColumn(
                name: "TopicModel",
                table: "ProcessedComments");
        }
    }
}
