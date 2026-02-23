using Microsoft.EntityFrameworkCore;
using StudentEvalSentiment.DB.Context;
using StudentEvalSentiment.Models.Entities.Analytics;
using System.Text.Json;

namespace StudentEvalSentiment.Services
{
    public sealed class TopicLabelSeeder
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

        private sealed record Payload(string label, string? notes);

        public async Task UpsertFromJsonAsync(CancellationToken ct = default)
        {
            var path = Path.Combine(_env.ContentRootPath, "topic-labels.json"); // or labeled.json
            if (!File.Exists(path))
            {
                _logger.LogWarning("Topic labels file not found: {Path}", path);
                return;
            }

            var json = await File.ReadAllTextAsync(path, ct);
            var root = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Payload>>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (root is null || root.Count == 0)
            {
                _logger.LogWarning("Topic labels JSON empty/invalid.");
                return;
            }

            int inserted = 0, updated = 0;

            foreach (var (targetType, clusters) in root)
            {
                foreach (var (clusterIdStr, payload) in clusters)
                {
                    if (!int.TryParse(clusterIdStr, out var clusterId)) continue;

                    var newLabel = (payload.label ?? "").Trim();
                    var newNotes = payload.notes;

                    if (string.IsNullOrWhiteSpace(newLabel)) continue;

                    var row = await _db.TopicClusters
                        .FirstOrDefaultAsync(t => t.TargetType == targetType && t.TopicClusterId == clusterId, ct);

                    if (row is null)
                    {
                        _db.TopicClusters.Add(new TopicCluster
                        {
                            TargetType = targetType,
                            TopicClusterId = clusterId,     // now allowed (ValueGeneratedNever)
                            HumanLabel = newLabel,
                            Notes = newNotes,
                            CreatedUtc = DateTime.UtcNow
                        });
                        inserted++;
                    }
                    else
                    {
                        var changed = false;

                        if (!string.Equals(row.HumanLabel, newLabel, StringComparison.Ordinal))
                        {
                            row.HumanLabel = newLabel;
                            changed = true;
                        }

                        if (newNotes is not null && !string.Equals(row.Notes, newNotes, StringComparison.Ordinal))
                        {
                            row.Notes = newNotes;
                            changed = true;
                        }

                        if (changed) updated++;
                    }
                }
            }

            if (inserted > 0 || updated > 0)
            {
                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Topic label upsert complete. Inserted={Inserted}, Updated={Updated}", inserted, updated);
        }
    }
}
