using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using StudentEvalSentiment.DB.Context;
using StudentEvalSentiment.Models.Ml.Sentiment;
using StudentEvalSentiment.Models.Ml.Topics;

namespace StudentEvalSentiment.Controllers;

[ApiController]
[Route("api/analyze")]
public class AnalyzeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PredictionEnginePool<SentimentModelInput, SentimentModelOutput> _sentimentPool;
    private readonly PredictionEnginePool<TopicModelInput, TopicModelOutput> _topicPool;

    public AnalyzeController(
        AppDbContext db,
        PredictionEnginePool<SentimentModelInput, SentimentModelOutput> sentimentPool,
        PredictionEnginePool<TopicModelInput, TopicModelOutput> topicPool)
    {
        _db = db;
        _sentimentPool = sentimentPool;
        _topicPool = topicPool;
    }

    [HttpPost("batch/{importBatchId:guid}")]
    public async Task<IActionResult> AnalyzeBatch(Guid importBatchId, CancellationToken ct)
    {
        // Pull rows for this batch (only those missing sentiment/topic if you want)
        var comments = await _db.ProcessedComments
            .Where(c => c.ImportBatchId == importBatchId)
            .ToListAsync(ct);

        if (comments.Count == 0)
            return NotFound("No ProcessedComments found for this batch.");

        int sentimentUpdated = 0;
        int topicsUpdated = 0;
        var now = DateTime.UtcNow;

        foreach (var c in comments)
        {
            // ---- Sentiment ----
            if (string.IsNullOrWhiteSpace(c.SentimentLabel))
            {
                var s = _sentimentPool.Predict("SentimentModel", new SentimentModelInput
                {
                    TextClean = c.TextClean ?? "",
                    Label = "" // dummy required by your pipeline
                });

                c.SentimentLabel = s.PredictedLabel;
                c.SentimentAssignedUtc = now;
                sentimentUpdated++;
            }

            // ---- Topic ----
            if (c.TopicClusterId == null)
            {
                var modelName = string.Equals(c.TargetType, "Instructor", StringComparison.OrdinalIgnoreCase)
                    ? "TopicInstructorModel"
                    : "TopicCourseModel";

                var t = _topicPool.Predict(modelName, new TopicModelInput
                {
                    TextClean = c.TextClean ?? ""
                });

                // Use the cluster id property from your TopicModelOutput
                c.TopicClusterId = (int)t.ClusterId;   // <- IMPORTANT
                c.TopicAssignedUtc = now;
                topicsUpdated++;
            }
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            importBatchId,
            total = comments.Count,
            sentimentUpdated,
            topicsUpdated
        });
    }
}
