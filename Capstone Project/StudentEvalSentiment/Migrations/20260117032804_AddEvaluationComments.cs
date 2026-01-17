using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentEvalSentiment.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationComments",
                columns: table => new
                {
                    CommentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetType = table.Column<byte>(type: "tinyint", nullable: false),
                    InstructorId = table.Column<int>(type: "int", nullable: true),
                    TextResponseId = table.Column<long>(type: "bigint", nullable: true),
                    QuestionId = table.Column<int>(type: "int", nullable: true),
                    TextAnonymized = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TextClean = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationComments", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_EvaluationComments_Instructor_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Instructor",
                        principalColumn: "InstructorId");
                    table.ForeignKey(
                        name: "FK_EvaluationComments_Section_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Section",
                        principalColumn: "SectionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationComments_TextResponses_TextResponseId",
                        column: x => x.TextResponseId,
                        principalTable: "TextResponses",
                        principalColumn: "TextResponseId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationComments_InstructorId",
                table: "EvaluationComments",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationComments_SectionId",
                table: "EvaluationComments",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationComments_TargetType",
                table: "EvaluationComments",
                column: "TargetType");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationComments_TextResponseId",
                table: "EvaluationComments",
                column: "TextResponseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationComments");
        }
    }
}
