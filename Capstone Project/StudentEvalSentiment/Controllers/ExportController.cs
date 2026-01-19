using Microsoft.AspNetCore.Mvc;
using StudentEvalSentiment.DB.Context;
using StudentEvalSentiment.Services;

namespace StudentEvalSentiment.Controllers
{
    [ApiController]
    [Route("export")]
    public class ExportController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ExportController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("raw-comments")]
        public async Task<IActionResult> ExportRawComments(CancellationToken ct)
        {
            // Create CSV in-memory
            var csvPath = Path.Combine(Path.GetTempPath(), "raw_comments_export.csv");
            await CsvExport.ExportRawCommentsAsync(_db, csvPath, ct);

            var bytes = await System.IO.File.ReadAllBytesAsync(csvPath, ct);
            return File(bytes, "text/csv", "raw_comments_export.csv");
        }
    }
}
