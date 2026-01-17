using Microsoft.EntityFrameworkCore;
using StudentEvalSentiment.DB.Context;
using StudentEvalSentiment.Models.Entities.Evaluations;
using StudentEvalSentiment.Models.Entities.Survey;

namespace StudentEvalSentiment.Services
{
    public class CommentBuildService
    {
        private readonly AppDbContext _db;

        public CommentBuildService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<int> BuildEvaluationCommentsAsync(CancellationToken ct = default)
        {
            // Load source rows (raw text responses) with needed joins
            var source = await (
                from tr in _db.TextResponses.AsNoTracking()
                join tgt in _db.EvaluationTargets.AsNoTracking() on tr.TargetId equals tgt.TargetId
                join sub in _db.EvaluationSubmissions.AsNoTracking() on tgt.SubmissionId equals sub.SubmissionId
                select new
                {
                    TextResponseId = tr.TextResponseId,
                    tr.QuestionId,
                    tr.RawText,
                    TargetId = tgt.TargetId,
                    tgt.TargetType,
                    tgt.InstructorId,
                    SectionId = sub.SectionId,
                    SubmittedAt = sub.SubmittedAt
                }
            ).ToListAsync(ct);

            // OPTIONAL: avoid duplicates if you re-run (by TextResponseId)
            var existing = await _db.EvaluationComments
                .AsNoTracking()
                .Select(x => x.TextResponseId)
                .ToHashSetAsync(ct);

            var toInsert = new List<EvaluationComment>(capacity: source.Count);

            foreach (var s in source)
            {
                if (s.RawText == null) continue;
                if (string.IsNullOrWhiteSpace(s.RawText)) continue;
                if (existing.Contains(s.TextResponseId)) continue;

                // For now, store raw into anonymized/clean as placeholders.
                // You'll replace these with real preprocessing output later (C# or Python pipeline).
                var anonymized = s.RawText; // TODO: replace with anonymizer
                var clean = s.RawText;      // TODO: replace with cleaner

                toInsert.Add(new EvaluationComment
                {
                    SectionId = s.SectionId,
                    SubmissionId = null,            // if you added it, set to s.SubmissionId (add to projection)
                    TargetId = s.TargetId,
                    TargetType = s.TargetType,
                    InstructorId = s.InstructorId,
                    TextResponseId = s.TextResponseId,
                    QuestionId = s.QuestionId,
                    TextAnonymized = anonymized,
                    TextClean = clean,
                    SubmittedAt = s.SubmittedAt
                });
            }

            if (toInsert.Count == 0) return 0;

            _db.EvaluationComments.AddRange(toInsert);
            return await _db.SaveChangesAsync(ct);
        }
    }
}
