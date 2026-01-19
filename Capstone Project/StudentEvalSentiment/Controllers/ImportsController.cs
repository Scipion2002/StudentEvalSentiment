using Microsoft.AspNetCore.Mvc;
using StudentEvalSentiment.DB.Context;
using StudentEvalSentiment.Models.Entities.Staging;
using System.Diagnostics;

namespace StudentEvalSentiment.Controllers
{
    [ApiController]
    [Route("imports")]
    public class ImportsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ImportsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("course-evals")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> UploadAndProcess([FromForm] IFormFile file, CancellationToken ct)
        {
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
            var pythonExe = "python"; // or full path to your venv python.exe
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "python", "processor.py");

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

            // 3) Read output CSV and insert
            if (!System.IO.File.Exists(outputPath))
                return StatusCode(500, "Python did not produce output CSV.");

            var processed = ReadProcessedCsv(outputPath, importBatchId, file.FileName);

            _db.ProcessedComments.AddRange(processed);
            var inserted = await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                importBatchId,
                insertedRows = inserted,
                pythonStdout = stdout
            });
        }

        private static List<ProcessedComment> ReadProcessedCsv(string csvPath, Guid importBatchId, string sourceFileName)
        {
            // Minimal CSV parsing. Assumes no commas inside fields OR that Python quotes fields correctly.
            // If you want robust CSV parsing, use CsvHelper package (recommended).
            var lines = System.IO.File.ReadAllLines(csvPath);
            if (lines.Length < 2) return new List<ProcessedComment>();

            var header = lines[0].Split(',');
            int idx(string name) => Array.FindIndex(header, h => string.Equals(h.Trim(), name, StringComparison.OrdinalIgnoreCase));

            var iTarget = idx("TargetType");
            var iInstr = idx("InstructorName");
            var iCNum = idx("CourseNumber");
            var iCName = idx("CourseName");
            var iRaw = idx("RawText");     // optional
            var iClean = idx("TextClean");   // required

            if (iTarget < 0 || iClean < 0)
                throw new InvalidOperationException("Output CSV missing required columns: TargetType, TextClean");

            var list = new List<ProcessedComment>();

            // NOTE: This simple parser will break on commas inside quoted strings.
            // If you expect that, tell me and I’ll swap this to CsvHelper.
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length <= Math.Max(iTarget, iClean)) continue;

                string get(int ix) => (ix >= 0 && ix < parts.Length) ? parts[ix].Trim().Trim('"') : "";

                var clean = get(iClean);
                if (string.IsNullOrWhiteSpace(clean)) continue;

                list.Add(new ProcessedComment
                {
                    ImportBatchId = importBatchId,
                    SourceFileName = sourceFileName,
                    TargetType = get(iTarget),
                    InstructorName = get(iInstr),
                    CourseNumber = get(iCNum),
                    CourseName = get(iCName),
                    RawText = iRaw >= 0 ? get(iRaw) : null,
                    TextClean = clean
                });
            }

            return list;
        }
    }
}
