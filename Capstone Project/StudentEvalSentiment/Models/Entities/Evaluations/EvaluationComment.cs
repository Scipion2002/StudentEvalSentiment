using StudentEvalSentiment.Models.Entities.Academic;
using StudentEvalSentiment.Models.Entities.Survey;

namespace StudentEvalSentiment.Models.Entities.Evaluations
{
    public class EvaluationComment
    {
        public long CommentId { get; set; }

        // Context
        public int SectionId { get; set; }
        public Guid? SubmissionId { get; set; }     // optional link back to submission
        public Guid? TargetId { get; set; }         // optional link back to EvaluationTarget

        public TargetType TargetType { get; set; }  // Instructor / Course
        public int? InstructorId { get; set; }      // null for Course targets (optional)

        // Source traceability (optional)
        public long? TextResponseId { get; set; }   // link to original TextResponse
        public int? QuestionId { get; set; }        // which question produced this comment

        // Text fields
        public string TextAnonymized { get; set; } = null!; // after [EMAIL]/[PHONE]/[URL], maybe NER
        public string TextClean { get; set; } = null!;      // stopwords/punct removed

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }

        // Navigation (optional but useful)
        public Section Section { get; set; } = null!;
        public Instructor? Instructor { get; set; }
        public TextResponse? TextResponse { get; set; }
    }
}
