using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEvalSentiment.DB.Context;
using StudentEvalSentiment.Models.Insights;
using static StudentEvalSentiment.Models.DTOs.Drilldown.DrilldownDTOs;

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

        [HttpGet("sentiment-drilldown")]
        public async Task<ActionResult<SentimentDrilldownResponseDto>> SentimentDrilldown(
        [FromQuery] string targetType = "Instructor",
        [FromQuery] Guid? importBatchId = null,
        [FromQuery] string? instructorName = null,
        [FromQuery] string? courseNumber = null,
        [FromQuery] int? topicClusterId = null,
        [FromQuery] string sentiment = "Negative",
        [FromQuery] int takePerQuestion = 25
        )
        {
            targetType = NormalizeTargetType(targetType);
            sentiment = NormalizeSentiment(sentiment);

            var pc = _db.ProcessedComments.AsNoTracking()
                .Where(x => x.TargetType == targetType)
                .Where(x => x.SentimentLabel == sentiment);

            if (importBatchId is not null)
                pc = pc.Where(x => x.ImportBatchId == importBatchId);

            if (!string.IsNullOrWhiteSpace(instructorName))
                pc = pc.Where(x => x.InstructorName == instructorName);

            if (!string.IsNullOrWhiteSpace(courseNumber))
                pc = pc.Where(x => x.CourseNumber == courseNumber);

            if (topicClusterId is not null && topicClusterId != 0)
                pc = pc.Where(x => x.TopicClusterId == topicClusterId);

            var total = await pc.CountAsync();

            // Pull the top QuestionKeys (by volume) first
            var topKeys = await pc
                .GroupBy(x => x.QuestionKey)
                .Select(g => new { QuestionKey = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var keys = topKeys.Select(x => x.QuestionKey).ToList();

            // Lookup headers
            var headers = await _db.ProcessedQuestionMaps.AsNoTracking()
                .Where(q => keys.Contains(q.QuestionKey))
                .ToDictionaryAsync(q => q.QuestionKey, q => q.QuestionHeader);

            // Pull answers for those keys (limit per question)
            // We'll do this in-memory grouping after fetching a reasonable set.
            // If your dataset is huge, we can optimize further.
            var answers = await pc
                .Where(x => keys.Contains(x.QuestionKey))
                .Select(x => new { x.QuestionKey, x.ProcessedCommentId, x.RawText })
                .OrderBy(x => x.QuestionKey)
                .ThenByDescending(x => x.ProcessedCommentId)
                .ToListAsync();

            var grouped = answers
                .GroupBy(x => x.QuestionKey)
                .Select(g =>
                {
                    var header = headers.TryGetValue(g.Key, out var h) ? h : g.Key;
                    var topAnswers = g
                        .Where(a => !string.IsNullOrWhiteSpace(a.RawText))
                        .Take(takePerQuestion)
                        .Select(a => new DrilldownAnswerDto(a.ProcessedCommentId, a.RawText))
                        .ToList();

                    var count = topKeys.First(k => k.QuestionKey == g.Key).Count;

                    return new DrilldownQuestionDto(
                        QuestionKey: g.Key,
                        QuestionHeader: header,
                        Count: count,
                        Answers: topAnswers
                    );
                })
                .OrderByDescending(q => q.Count)
                .ToList();

            return new SentimentDrilldownResponseDto(
                Sentiment: sentiment,
                TotalAnswers: total,
                Questions: grouped
            );
        }

        private static string NormalizeTargetType(string s)
            => string.Equals(s, "course", StringComparison.OrdinalIgnoreCase) ? "Course" : "Instructor";

        private static string NormalizeSentiment(string s)
        {
            if (string.Equals(s, "positive", StringComparison.OrdinalIgnoreCase)) return "Positive";
            if (string.Equals(s, "neutral", StringComparison.OrdinalIgnoreCase)) return "Neutral";
            return "Negative";
        }
    }
}
