using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentEvalSentiment.Migrations
{
    /// <inheritdoc />
    public partial class AddImportBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "RowCountImported",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "RowCountTotal",
                table: "ImportBatches");

            migrationBuilder.RenameColumn(
                name: "ImportedAt",
                table: "ImportBatches",
                newName: "CreatedUtc");

            migrationBuilder.AlterColumn<decimal>(
                name: "Dim5Avg",
                table: "ProcessedLikertSummaries",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Dim4Avg",
                table: "ProcessedLikertSummaries",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Dim3Avg",
                table: "ProcessedLikertSummaries",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Dim2Avg",
                table: "ProcessedLikertSummaries",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Dim1Avg",
                table: "ProcessedLikertSummaries",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "SourceFileName",
                table: "ImportBatches",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "FileHashSha256",
                table: "ImportBatches",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "ImportBatches",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_FileHashSha256",
                table: "ImportBatches",
                column: "FileHashSha256",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportBatches_FileHashSha256",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "FileHashSha256",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "ImportBatches");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "ImportBatches",
                newName: "ImportedAt");

            migrationBuilder.AlterColumn<decimal>(
                name: "Dim5Avg",
                table: "ProcessedLikertSummaries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Dim4Avg",
                table: "ProcessedLikertSummaries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Dim3Avg",
                table: "ProcessedLikertSummaries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Dim2Avg",
                table: "ProcessedLikertSummaries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Dim1Avg",
                table: "ProcessedLikertSummaries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceFileName",
                table: "ImportBatches",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(260)",
                oldMaxLength: 260);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ImportBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RowCountImported",
                table: "ImportBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RowCountTotal",
                table: "ImportBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
