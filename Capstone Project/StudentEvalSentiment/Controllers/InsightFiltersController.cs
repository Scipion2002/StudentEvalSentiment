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

            var pc = _db.ProcessedComments.AsNoTracking()
                .Where(x => x.TargetType == targetType);

            if (importBatchId is not null)
                pc = pc.Where(x => x.ImportBatchId == importBatchId);

            // OPTIONAL: topic narrows the universe for dependent filters too
            if (topicClusterId is not null)
                pc = pc.Where(x => x.TopicClusterId == topicClusterId);

            // instructors list SHOULD depend on selected course/topic/batch (but NOT on instructor itself)
            var pcForInstructors = pc;

            // if course is selected, narrow instructors to those who appear on that course
            if (!string.IsNullOrWhiteSpace(courseNumber))
                pcForInstructors = pcForInstructors.Where(x => x.CourseNumber == courseNumber);

            // if topic is selected, narrow instructors to those who have that topic
            if (topicClusterId is not null && topicClusterId != 0)
                pcForInstructors = pcForInstructors.Where(x => x.TopicClusterId == topicClusterId);

            // NOTE: we do NOT filter by instructorName here (otherwise list becomes 1 item)

            var instructors = await pcForInstructors
                .Where(x => x.InstructorName != "")
                .Select(x => x.InstructorName)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // courses list SHOULD depend on selected instructor (when in Instructor mode)
            var pcForCourses = pc;

            if (targetType == "Instructor" && !string.IsNullOrWhiteSpace(instructorName))
                pcForCourses = pcForCourses.Where(x => x.InstructorName == instructorName);

            // (if you ever allow selecting course first, you can symmetrically filter instructors)
            if (targetType == "Course" && !string.IsNullOrWhiteSpace(courseNumber))
                pcForCourses = pcForCourses.Where(x => x.CourseNumber == courseNumber);

            var courses = await pcForCourses
                .Where(x => x.CourseNumber != "")
                .Select(x => new { x.CourseNumber, x.CourseName })
                .Distinct()
                .OrderBy(x => x.CourseNumber)
                .Select(x => new CourseOptionDto(x.CourseNumber, x.CourseName))
                .ToListAsync();

            // batches list (optionally depends on instructor selection too)
            var pcForBatches = pc;
            if (targetType == "Instructor" && !string.IsNullOrWhiteSpace(instructorName))
                pcForBatches = pcForBatches.Where(x => x.InstructorName == instructorName);

            var importBatches = await pcForBatches
                .Select(x => x.ImportBatchId)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();

            // topics list SHOULD depend on selected filters
            var pcForTopics = pc;

            // Apply both if provided (no targetType gating)
            if (!string.IsNullOrWhiteSpace(instructorName))
                pcForTopics = pcForTopics.Where(x => x.InstructorName == instructorName);

            if (!string.IsNullOrWhiteSpace(courseNumber))
                pcForTopics = pcForTopics.Where(x => x.CourseNumber == courseNumber);

            // If TopicClusterId is 0 when "unset", handle that too
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
