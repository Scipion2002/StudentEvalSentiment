using StudentEvalSentiment.Models.Entities.Academic;
using StudentEvalSentiment.Models.Entities.Survey;

namespace StudentEvalSentiment.Models.Entities.Evaluations
{
    public class EvaluationTarget
    {
        public Guid TargetId { get; set; } = Guid.NewGuid();
        public Guid SubmissionId { get; set; }

        public TargetType TargetType { get; set; }
        public int? InstructorId { get; set; }
        public int TargetOrder { get; set; }
        public string? DisplayLabel { get; set; }

        public EvaluationSubmission Submission { get; set; } = null!;
        public Instructor? Instructor { get; set; }
        public ICollection<LikertResponse> LikertResponses { get; set; } = new List<LikertResponse>();
        public ICollection<TextResponse> TextResponses { get; set; } = new List<TextResponse>();
    }
}
