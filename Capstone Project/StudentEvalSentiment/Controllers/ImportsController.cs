using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudentEvalSentiment.DB.Context;
using StudentEvalSentiment.Models.DTOs;
using StudentEvalSentiment.Models.Entities.Staging;
using StudentEvalSentiment.Models.Requests;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Globalization;

namespace StudentEvalSentiment.Controllers
{
    [ApiController]
    [Route("imports")]
    public class ImportsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public ImportsController(AppDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        
        [HttpPost("course-evals")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> UploadAndProcess([FromForm] CourseEvalUploadRequest request, CancellationToken ct)
        {
            var file = request.File;

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var importBatchId = Guid.NewGuid();
            var tempDir = Path.Combine(Path.GetTempPath(), "StudentEvalSentiment", importBatchId.ToString("N"));
            Directory.CreateDirectory(tempDir);

            var inputPath = Path.Combine(tempDir, file.FileName);
            var outputPath = Path.Combine(tempDir, "sentiment_text_clean.csv");

            // 1) Save upload
            await using (var fs = System.IO.File.Create(inputPath))
                await file.CopyToAsync(fs, ct);

            // 2) Run Python script
            // Update these two paths for your machine/deployment:
            var pythonExe = _configuration["Python:ExecutablePath"];

            var scriptPath = Path.Combine(
                AppContext.BaseDirectory,
                "Python",
                "processor.py"
            );

            if (!System.IO.File.Exists(pythonExe))
                return StatusCode(500, $"Python exe not found: {pythonExe}");

            if (!System.IO.File.Exists(scriptPath))
                return StatusCode(500, $"processor.py not found: {scriptPath}");

            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\" \"{inputPath}\" \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return StatusCode(500, "Failed to start python process.");

            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode != 0)
            {
                return StatusCode(500, $"Python failed.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
            }

            // 3) Read output CSV
            if (!System.IO.File.Exists(outputPath))
                return StatusCode(500, "Python did not produce output CSV.");

            // ✅ Read rows (DTO)
            var csvRows = ReadProcessedCsvRows(outputPath);

            // ✅ Upsert question map
            await UpsertQuestionMapAsync(csvRows, ct);

            // ✅ Insert processed comments (drop QuestionHeader here)
            var comments = ToProcessedComments(csvRows, importBatchId, file.FileName);
            _db.ProcessedComments.AddRange(comments);

            var inserted = await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                importBatchId,
                insertedRows = comments.Count, // better than EF's inserted count
                pythonStdout = stdout
            });
        }

        private async Task UpsertQuestionMapAsync(IEnumerable<ProcessedCommentCsvRow> rows, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            var pairs = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.QuestionKey) && !string.IsNullOrWhiteSpace(r.QuestionHeader))
                .GroupBy(r => r.QuestionKey)
                .Select(g => new { QuestionKey = g.Key, QuestionHeader = g.First().QuestionHeader })
                .ToList();

            if (pairs.Count == 0) return;

            var keys = pairs.Select(p => p.QuestionKey).ToList();
            var existing = await _db.ProcessedQuestionMaps
                .Where(x => keys.Contains(x.QuestionKey))
                .ToDictionaryAsync(x => x.QuestionKey, ct);

            foreach (var p in pairs)
            {
                if (!existing.TryGetValue(p.QuestionKey, out var map))
                {
                    _db.ProcessedQuestionMaps.Add(new ProcessedQuestionMap
                    {
                        QuestionKey = p.QuestionKey,
                        QuestionHeader = p.QuestionHeader,
                        FirstSeenUtc = now,
                        LastSeenUtc = now
                    });
                }
                else
                {
                    map.LastSeenUtc = now;
                    if (map.QuestionHeader != p.QuestionHeader)
                        map.QuestionHeader = p.QuestionHeader;
                }
            }
        }

        private static List<ProcessedCommentCsvRow> ReadProcessedCsvRows(string csvPath)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null,
                MissingFieldFound = null,
                HeaderValidated = null,
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args => args.Header.Trim(),
                TrimOptions = TrimOptions.Trim
            };

            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, config);

            if (!csv.Read()) return new();
            csv.ReadHeader();

            var rows = new List<ProcessedCommentCsvRow>();

            while (csv.Read())
            {
                var clean = csv.GetField("TextClean");
                if (string.IsNullOrWhiteSpace(clean)) continue;

                rows.Add(new ProcessedCommentCsvRow
                {
                    TargetType = csv.GetField("TargetType") ?? "",
                    InstructorName = csv.GetField("InstructorName") ?? "",
                    CourseNumber = csv.GetField("CourseNumber") ?? "",
                    CourseName = csv.GetField("CourseName") ?? "",
                    QuestionKey = csv.GetField("QuestionKey") ?? "",
                    QuestionHeader = csv.GetField("QuestionHeader") ?? "",
                    RawText = csv.TryGetField("RawText", out string? raw) ? raw : null,
                    TextClean = clean
                });
            }

            return rows;
        }

        private static List<ProcessedComment> ToProcessedComments(
        IEnumerable<ProcessedCommentCsvRow> rows,
        Guid importBatchId,
        string sourceFileName)
        {
            return rows.Select(r => new ProcessedComment
            {
                ImportBatchId = importBatchId,
                SourceFileName = sourceFileName,
                TargetType = r.TargetType,
                InstructorName = r.InstructorName,
                CourseNumber = r.CourseNumber,
                CourseName = r.CourseName,
                QuestionKey = r.QuestionKey,
                RawText = r.RawText,
                TextClean = r.TextClean
            }).ToList();
        }

        private static List<ProcessedComment> ReadProcessedCsv(string csvPath, Guid importBatchId, string sourceFileName)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null,
                MissingFieldFound = null,
                HeaderValidated = null,
                IgnoreBlankLines = true,
                DetectDelimiter = false,
                Delimiter = ",",
                PrepareHeaderForMatch = args => args.Header.Trim(),
                TrimOptions = TrimOptions.Trim
            };

            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, config);

            // ✅ Read header first
            if (!csv.Read())
                return new List<ProcessedComment>();
            csv.ReadHeader();

            var records = new List<ProcessedComment>();

            while (csv.Read())
            {
                var clean = csv.GetField("TextClean");
                if (string.IsNullOrWhiteSpace(clean)) continue;

                records.Add(new ProcessedComment
                {
                    ImportBatchId = importBatchId,
                    SourceFileName = sourceFileName,
                    TargetType = csv.GetField("TargetType") ?? "",
                    InstructorName = csv.GetField("InstructorName") ?? "",
                    CourseNumber = csv.GetField("CourseNumber") ?? "",
                    CourseName = csv.GetField("CourseName") ?? "",
                    RawText = csv.TryGetField("RawText", out string? raw) ? raw : null,
                    TextClean = clean
                });
            }

            return records;
        }
    }
}
