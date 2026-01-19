using Microsoft.EntityFrameworkCore;
using StudentEvalSentiment.DB.Context;
using System.Text;

namespace StudentEvalSentiment.Services
{
    public class CsvExport
    {
        private static string CsvEscape(string? s)
        {
            s ??= "";
            s = s.Replace("\"", "\"\"");
            return $"\"{s}\"";
        }

        public static async Task<string> ExportRawCommentsAsync(AppDbContext db, string outputPath, CancellationToken ct = default)
        {
            var rows = await db.EvaluationComments
                .AsNoTracking()
                .Select(c => new
                {
                    c.CommentId,
                    c.TargetType,
                    c.TextAnonymized, // if you don't store raw, use this
                    InstructorName = c.Instructor != null ? c.Instructor.DisplayName : "",
                    CourseNumber = c.Section.Course.CourseNumber,
                    CourseName = c.Section.Course.CourseName
                })
                .ToListAsync(ct);

            var sb = new StringBuilder();
            sb.AppendLine("CommentId,TargetType,InstructorName,CourseNumber,CourseName,RawText");

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",",
                    r.CommentId,
                    (byte)r.TargetType,
                    CsvEscape(r.InstructorName),
                    CsvEscape(r.CourseNumber),
                    CsvEscape(r.CourseName),
                    CsvEscape(r.TextAnonymized)   // treat as "raw input for python"
                ));
            }

            await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8, ct);
            return outputPath;
        }
    }
}
