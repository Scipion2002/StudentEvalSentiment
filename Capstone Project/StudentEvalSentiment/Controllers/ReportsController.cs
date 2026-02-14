using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEvalSentiment.DB.Context;

namespace StudentEvalSentiment.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReportsController(AppDbContext db) => _db = db;

        [HttpGet("batch/{importBatchId:guid}/sentiment-counts")]
        public async Task<IActionResult> SentimentCounts(Guid importBatchId, string targetType = "Instructor", CancellationToken ct = default)
        {
            targetType = targetType.Trim();

            var data = await _db.ProcessedComments
                .Where(x => x.ImportBatchId == importBatchId && x.TargetType == targetType)
                .GroupBy(x => x.SentimentLabel)
                .Select(g => new { sentiment = g.Key ?? "Unknown", count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToListAsync(ct);

            return Ok(data);
        }

        [HttpGet("batch/{importBatchId:guid}/top-topics")]
        public async Task<IActionResult> TopTopics(Guid importBatchId, string targetType = "Instructor", int top = 10, CancellationToken ct = default)
        {
            targetType = targetType.Trim();

            var query =
                from pc in _db.ProcessedComments
                where pc.ImportBatchId == importBatchId
                   && pc.TargetType == targetType
                   && pc.TopicClusterId != null
                join tc in _db.TopicClusters
                  on new { Id = pc.TopicClusterId!.Value, Target = pc.TargetType }
                  equals new { Id = tc.TopicClusterId, Target = tc.TargetType }
                  into tcJoin
                from tc in tcJoin.DefaultIfEmpty()
                group tc by new { pc.TopicClusterId, Label = tc != null ? tc.HumanLabel : null } into g
                orderby g.Count() descending
                select new
                {
                    topicClusterId = g.Key.TopicClusterId,
                    humanLabel = g.Key.Label ?? "(unlabeled)",
                    count = g.Count()
                };

            var data = await query.Take(top).ToListAsync(ct);
            return Ok(data);
        }
    }
}
