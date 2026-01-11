using StudentEvalSentiment.Models.Entities.Survey;

namespace StudentEvalSentiment.Models.Entities.Evaluations
{
    public class LikertResponse
    {
        public Guid TargetId { get; set; }
        public int QuestionId { get; set; }

        public byte? Value { get; set; }
        public bool IsApplicable { get; set; } = true;

        public EvaluationTarget Target { get; set; } = null!;
        public SurveyQuestion Question { get; set; } = null!;
    }
}
