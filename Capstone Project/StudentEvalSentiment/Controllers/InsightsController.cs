using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEvalSentiment.DB.Context;
using StudentEvalSentiment.Models.Insights;

namespace StudentEvalSentiment.Controllers
{
    [ApiController]
    [Route("api/insights")]
    public sealed class InsightsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public InsightsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<InsightsResponseDto>> Get([FromQuery] InsightQuery q)
        {
            var targetType = NormalizeTargetType(q.TargetType);

            var pc = _db.ProcessedComments.AsNoTracking()
                .Where(x => x.TargetType == targetType);

            // Trimester (batch)
            if (q.ImportBatchId is not null)
                pc = pc.Where(x => x.ImportBatchId == q.ImportBatchId);

            // Entity filters
            if (targetType == "Instructor" && !string.IsNullOrWhiteSpace(q.InstructorName))
                pc = pc.Where(x => x.InstructorName == q.InstructorName);

            if (targetType == "Course" && !string.IsNullOrWhiteSpace(q.CourseNumber))
                pc = pc.Where(x => x.CourseNumber == q.CourseNumber);

            // Topic drilldown
            if (q.TopicClusterId is not null)
                pc = pc.Where(x => x.TopicClusterId == q.TopicClusterId);

            // 1) Sentiment breakdown
            var sentiment = await pc
                .GroupBy(_ => 1)
                .Select(g => new SentimentBreakdownDto(
                    Positive: g.Count(x => x.SentimentLabel == "Positive"),
                    Neutral: g.Count(x => x.SentimentLabel == "Neutral"),
                    Negative: g.Count(x => x.SentimentLabel == "Negative")
                ))
                .FirstOrDefaultAsync() ?? new SentimentBreakdownDto(0, 0, 0);

            // 2) Top topics (join TopicClusters for labels)
            var topTopics = await (
                from c in pc
                where c.TopicClusterId != null
                join t in _db.TopicClusters.AsNoTracking()
                    on new { TopicClusterId = c.TopicClusterId!.Value, TargetType = c.TargetType }
                    equals new { t.TopicClusterId, t.TargetType }
                    into tj
                from t in tj.DefaultIfEmpty()
                group t by new { TopicClusterId = c.TopicClusterId!.Value, Label = (t != null ? t.HumanLabel : null) } into g
                orderby g.Count() descending
                select new TopicCountDto(
                    g.Key.TopicClusterId,
                    g.Key.Label ?? $"Topic {g.Key.TopicClusterId}",
                    g.Count()
                )
            )
            .Take(10)
            .ToListAsync();

            return new InsightsResponseDto(sentiment, topTopics);
        }

        private static string NormalizeTargetType(string s)
            => string.Equals(s, "course", StringComparison.OrdinalIgnoreCase) ? "Course" : "Instructor";
    }
}
