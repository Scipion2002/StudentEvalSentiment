using Microsoft.EntityFrameworkCore;
using StudentEvalSentiment.DB.Context;
using System.Text.Json;

namespace StudentEvalSentiment.Services
{
    public class TopicLabelSeeder
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<TopicLabelSeeder> _logger;

        public TopicLabelSeeder(AppDbContext db, IWebHostEnvironment env, ILogger<TopicLabelSeeder> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        private sealed record LabelPayload(string label, string? notes);

        public async Task UpsertFromJsonAsync(CancellationToken ct = default)
        {
            var path = Path.Combine(_env.ContentRootPath, "topic-labels.json");
            if (!File.Exists(path))
            {
                _logger.LogWarning("topic-labels.json not found at {Path}. Skipping topic label seeding.", path);
                return;
            }

            var json = await File.ReadAllTextAsync(path, ct);

            // Structure: { "Instructor": { "64": { "label": "...", "notes": "..." } }, "Course": { ... } }
            var root = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, LabelPayload>>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (root is null || root.Count == 0)
            {
                _logger.LogWarning("topic-labels.json is empty or invalid. Skipping.");
                return;
            }

            int updated = 0;

            foreach (var (targetType, clusters) in root)
            {
                foreach (var (clusterIdStr, payload) in clusters)
                {
                    if (!int.TryParse(clusterIdStr, out var clusterId))
                        continue;

                    var row = await _db.TopicClusters
                        .FirstOrDefaultAsync(t => t.TopicClusterId == clusterId && t.TargetType == targetType, ct);

                    if (row is null)
                        continue; // row must already exist from training/pipeline

                    var newLabel = (payload.label ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(newLabel))
                        continue;

                    // Only update if different (avoids pointless writes)
                    if (!string.Equals(row.HumanLabel, newLabel, StringComparison.Ordinal))
                    {
                        row.HumanLabel = newLabel;
                        updated++;
                    }

                    if (payload.notes is not null && !string.Equals(row.Notes, payload.notes, StringComparison.Ordinal))
                    {
                        row.Notes = payload.notes;
                    }
                }
            }

            if (updated > 0)
            {
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Topic label seeding complete. Updated {Count} TopicClusters.", updated);
            }
            else
            {
                _logger.LogInformation("Topic label seeding complete. No changes needed.");
            }
        }
    }
}
