namespace StudentEvalSentiment.Models.Entities.Survey
{
    public class SurveyQuestion
    {
        public int SurveyQuestionId { get; set; }

        public int SurveyId { get; set; }
        public Survey Survey { get; set; } = null!;

        // Stable key you generate (slug/hash). Helps with versioning.
        public string QuestionKey { get; set; } = null!;

        // Human-readable question text (canonical)
        public string CanonicalText { get; set; } = null!;

        public QuestionResponseType ResponseType { get; set; }
        public TargetType TargetType { get; set; }   // Instructor vs Course

        // Optional grouping (e.g., "Teaching", "Support", "Course Design")
        public string? Domain { get; set; }
    }
}
