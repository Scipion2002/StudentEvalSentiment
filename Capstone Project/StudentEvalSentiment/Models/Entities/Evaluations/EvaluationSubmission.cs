
using StudentEvalSentiment.Models.Entities.Academic;

namespace StudentEvalSentiment.Models.Entities.Evaluations
{
    public class EvaluationSubmission
    {
        public Guid SubmissionId { get; set; } = Guid.NewGuid();

        public int SectionId { get; set; }
        public int SurveyId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool? IsMobile { get; set; }

        public Guid? ImportBatchId { get; set; }
        public string? ExternalKey { get; set; }

        public Section Section { get; set; } = null!;
        public Survey.Survey Survey { get; set; } = null!;
        public ImportBatch? ImportBatch { get; set; }

        public ICollection<EvaluationTarget> Targets { get; set; } = new List<EvaluationTarget>();
    }
}
