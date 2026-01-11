using StudentEvalSentiment.Models.Entities.Survey;

namespace StudentEvalSentiment.Models.Entities.Evaluations
{
    public class TextResponse
    {
        public long TextResponseId { get; set; }
        public Guid TargetId { get; set; }
        public int QuestionId { get; set; }

        public string? RawText { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public EvaluationTarget Target { get; set; } = null!;
        public SurveyQuestion Question { get; set; } = null!;
    }
}
