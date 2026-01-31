using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudentEvalSentiment.DB.Context;
using StudentEvalSentiment.Models.DTOs;
using StudentEvalSentiment.Models.Entities.Evaluations;
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

            var fileHash = await ComputeSha256HexAsync(file, ct);

            var existing = await _db.ImportBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FileHashSha256 == fileHash, ct);

            if (existing != null)
            {
                return Ok(new
                {
                    importBatchId = existing.ImportBatchId,
                    skipped = true,
                    reason = "This exact file (by hash) was already imported."
                });
            }

            var importBatchId = Guid.NewGuid();

            //Saving import batch record
            ///////////////////////////////////////////////////////////////////////////

            try
            {
                _db.ImportBatches.Add(new ImportBatch
                {
                    ImportBatchId = importBatchId,
                    SourceFileName = file.FileName,
                    FileHashSha256 = fileHash,
                    FileSizeBytes = request.File.Length,
                    CreatedUtc = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(ct); // save batch early so it’s tracked
            }
            catch (DbUpdateException)
            {
                var existing2 = await _db.ImportBatches.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.FileHashSha256 == fileHash, ct);

                if (existing2 != null)
                    return Ok(new { importBatchId = existing2.ImportBatchId, skipped = true, reason = "Duplicate (hash) detected." });

                throw; // unexpected
            }

            ///////////////////////////////////////////////////////////////////////////


            var tempDir = Path.Combine(Path.GetTempPath(), "StudentEvalSentiment", importBatchId.ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {

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

                ///////////////////////////////////////////////////////////////////////////
                //New stuff, let me know if this is where is supposed to be
                var topicScript = Path.Combine(AppContext.BaseDirectory, "Python", "topic_prep.py");
                var topicArgs = $"\"{topicScript}\" \"{outputPath}\" \"{tempDir}\"";

                if (!System.IO.File.Exists(topicScript))
                    return StatusCode(500, $"topic_prep.py not found: {topicScript}");

                var topicPsi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = topicArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var topicProc = Process.Start(topicPsi);
                if (topicProc == null) return StatusCode(500, "Failed to start topic_prep python process.");

                var topicStdout = await topicProc.StandardOutput.ReadToEndAsync();
                var topicStderr = await topicProc.StandardError.ReadToEndAsync();
                await topicProc.WaitForExitAsync(ct);

                if (topicProc.ExitCode != 0)
                {
                    return StatusCode(500, $"topic_prep.py failed.\nSTDOUT:\n{topicStdout}\nSTDERR:\n{topicStderr}");
                }

                ///////////////////////////////////////////////////////////////////////////

                // ✅ Read rows (DTO)
                var csvRows = ReadProcessedCsvRows(outputPath);

                // ✅ Upsert question map
                await UpsertQuestionMapAsync(csvRows, ct);

                // ✅ Insert processed comments (drop QuestionHeader here)
                var comments = ToProcessedComments(csvRows, importBatchId, file.FileName);

                //////////////////////////////////////////////////////////////////////////

                // 4) Read and insert likert summary if exists
                var likertPath = Path.Combine(tempDir, "likert_summary.csv");
                if (System.IO.File.Exists(likertPath))
                {
                    var likertRows = ReadLikertSummaryCsv(likertPath);

                    var summaries = likertRows.Select(r => new ProcessedLikertSummary
                    {
                        ImportBatchId = importBatchId,
                        TargetType = r.TargetType,
                        InstructorName = string.IsNullOrWhiteSpace(r.InstructorName) ? null : r.InstructorName,
                        CourseNumber = r.CourseNumber,
                        CourseName = r.CourseName,
                        LikertAvg = r.LikertAvg,
                        LikertCountUsed = r.LikertCountUsed,
                        LabelDerived = r.LabelDerived,
                        Dim1Avg = r.Dim1Avg,
                        Dim2Avg = r.Dim2Avg,
                        Dim3Avg = r.Dim3Avg,
                        Dim4Avg = r.Dim4Avg,
                        Dim5Avg = r.Dim5Avg
                    }).ToList();

                    _db.ProcessedLikertSummaries.AddRange(summaries);
                }

                //////////////////////////////////////////////////////////////////////////

                // 5) Save to DB
                _db.ProcessedComments.AddRange(comments);

                // Save changes
                var inserted = await _db.SaveChangesAsync(ct);

                return Ok(new
                {
                    importBatchId,
                    insertedRows = comments.Count, // better than EF's inserted count
                    pythonStdout = stdout
                });
            }
            finally
            {
                // optional cleanup
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, recursive: true);
                }
                catch
                {
                    // swallow cleanup errors (file locks happen on Windows)
                }
            }
        }

        static async Task<string> ComputeSha256HexAsync(IFormFile file, CancellationToken ct)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            await using var stream = file.OpenReadStream();
            var hash = await sha.ComputeHashAsync(stream, ct);
            return Convert.ToHexString(hash).ToLowerInvariant(); // 64-char hex
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
                    SentimentLabel = csv.GetField("Label") ?? "",
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

        private static List<LikertSummaryCsvRow> ReadLikertSummaryCsv(string csvPath)
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

            var rows = new List<LikertSummaryCsvRow>();
            while (csv.Read())
            {
                rows.Add(new LikertSummaryCsvRow
                {
                    TargetType = csv.GetField("TargetType") ?? "",
                    InstructorName = csv.GetField("InstructorName") ?? "",
                    CourseNumber = csv.GetField("CourseNumber") ?? "",
                    CourseName = csv.GetField("CourseName") ?? "",
                    LikertAvg = csv.GetField<decimal>("LikertAvg"),
                    LikertCountUsed = csv.GetField<int>("LikertCountUsed"),
                    LabelDerived = csv.GetField("LabelDerived") ?? "",
                    Dim1Avg = csv.TryGetField<decimal?>("Dim1Avg", out var d1) ? d1 : null,
                    Dim2Avg = csv.TryGetField<decimal?>("Dim2Avg", out var d2) ? d2 : null,
                    Dim3Avg = csv.TryGetField<decimal?>("Dim3Avg", out var d3) ? d3 : null,
                    Dim4Avg = csv.TryGetField<decimal?>("Dim4Avg", out var d4) ? d4 : null,
                    Dim5Avg = csv.TryGetField<decimal?>("Dim5Avg", out var d5) ? d5 : null,
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
                SentimentLabel = r.SentimentLabel,
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
