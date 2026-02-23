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
    [FromQuery] Guid? importBatchId = null,
    [FromQuery] string? instructorName = null,
    [FromQuery] string? courseNumber = null,
    [FromQuery] int? topicClusterId = null
)
        {
            targetType = NormalizeTargetType(targetType);

            // Base universe: only target type (do NOT apply other filters here)
            var pcBase = _db.ProcessedComments.AsNoTracking()
                .Where(x => x.TargetType == targetType);

            // -------------------------
            // Instructors (all filters except instructorName)
            // -------------------------
            var pcForInstructors = pcBase;

            if (importBatchId is not null)
                pcForInstructors = pcForInstructors.Where(x => x.ImportBatchId == importBatchId);

            if (!string.IsNullOrWhiteSpace(courseNumber))
                pcForInstructors = pcForInstructors.Where(x => x.CourseNumber == courseNumber);

            if (topicClusterId is not null && topicClusterId != 0)
                pcForInstructors = pcForInstructors.Where(x => x.TopicClusterId == topicClusterId);

            var instructors = await pcForInstructors
                .Where(x => x.InstructorName != "")
                .Select(x => x.InstructorName)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // -------------------------
            // Courses (all filters except courseNumber)
            // -------------------------
            var pcForCourses = pcBase;

            if (importBatchId is not null)
                pcForCourses = pcForCourses.Where(x => x.ImportBatchId == importBatchId);

            if (!string.IsNullOrWhiteSpace(instructorName))
                pcForCourses = pcForCourses.Where(x => x.InstructorName == instructorName);

            if (topicClusterId is not null && topicClusterId != 0)
                pcForCourses = pcForCourses.Where(x => x.TopicClusterId == topicClusterId);

            var courses = await pcForCourses
                .Where(x => x.CourseNumber != "")
                .Select(x => new { x.CourseNumber, x.CourseName })
                .Distinct()
                .OrderBy(x => x.CourseNumber)
                .Select(x => new CourseOptionDto(x.CourseNumber, x.CourseName))
                .ToListAsync();

            // -------------------------
            // Import batches (all filters except importBatchId)
            // -------------------------
            var pcForBatches = pcBase;

            if (!string.IsNullOrWhiteSpace(instructorName))
                pcForBatches = pcForBatches.Where(x => x.InstructorName == instructorName);

            if (!string.IsNullOrWhiteSpace(courseNumber))
                pcForBatches = pcForBatches.Where(x => x.CourseNumber == courseNumber);

            if (topicClusterId is not null && topicClusterId != 0)
                pcForBatches = pcForBatches.Where(x => x.TopicClusterId == topicClusterId);

            var importBatches = await pcForBatches
                .Select(x => x.ImportBatchId)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();

            // -------------------------
            // Topics (all filters except topicClusterId)
            // -------------------------
            var pcForTopics = pcBase;

            if (importBatchId is not null)
                pcForTopics = pcForTopics.Where(x => x.ImportBatchId == importBatchId);

            if (!string.IsNullOrWhiteSpace(instructorName))
                pcForTopics = pcForTopics.Where(x => x.InstructorName == instructorName);

            if (!string.IsNullOrWhiteSpace(courseNumber))
                pcForTopics = pcForTopics.Where(x => x.CourseNumber == courseNumber);

            var topicIds = await pcForTopics
                .Where(x => x.TopicClusterId != null && x.TopicClusterId != 0)
                .Select(x => x.TopicClusterId!.Value)
                .Distinct()
                .ToListAsync();

            var topics = await _db.TopicClusters.AsNoTracking()
                .Where(t => t.TargetType == targetType && topicIds.Contains(t.TopicClusterId))
                .OrderBy(t => t.TopicClusterId)
                .Select(t => new TopicOptionDto(t.TopicClusterId, t.HumanLabel))
                .ToListAsync();

            return new InsightFiltersResponseDto(importBatches, instructors, courses, topics);
        }

        private static string NormalizeTargetType(string s)
            => string.Equals(s, "course", StringComparison.OrdinalIgnoreCase) ? "Course" : "Instructor";
    }
}
