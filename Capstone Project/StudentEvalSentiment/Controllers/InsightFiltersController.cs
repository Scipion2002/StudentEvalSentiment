using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEvalSentiment.DB.Context;
using StudentEvalSentiment.Models.Insights;

namespace StudentEvalSentiment.Controllers
{
    [ApiController]
    [Route("api/insight-filters")]
    public sealed class InsightFiltersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public InsightFiltersController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<InsightFiltersResponseDto>> Get(
            [FromQuery] string targetType = "Instructor",
            [FromQuery] Guid? importBatchId = null)
        {
            targetType = NormalizeTargetType(targetType);

            var pc = _db.ProcessedComments.AsNoTracking()
                .Where(x => x.TargetType == targetType);

            if (importBatchId is not null)
                pc = pc.Where(x => x.ImportBatchId == importBatchId);

            var importBatches = await pc
                .Select(x => x.ImportBatchId)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();

            var instructors = await pc
                .Where(x => x.InstructorName != "")
                .Select(x => x.InstructorName)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var courses = await pc
                .Where(x => x.CourseNumber != "")
                .Select(x => new { x.CourseNumber, x.CourseName })
                .Distinct()
                .OrderBy(x => x.CourseNumber)
                .Select(x => new CourseOptionDto(x.CourseNumber, x.CourseName))
                .ToListAsync();

            var topics = await _db.TopicClusters.AsNoTracking()
                .Where(t => t.TargetType == targetType)
                .OrderBy(t => t.TopicClusterId)
                .Select(t => new TopicOptionDto(t.TopicClusterId, t.HumanLabel))
                .ToListAsync();

            return new InsightFiltersResponseDto(importBatches, instructors, courses, topics);
        }

        private static string NormalizeTargetType(string s)
            => string.Equals(s, "course", StringComparison.OrdinalIgnoreCase) ? "Course" : "Instructor";
    }
}
