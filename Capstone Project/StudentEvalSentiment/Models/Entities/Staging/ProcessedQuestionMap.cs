namespace StudentEvalSentiment.Models.Entities.Staging
{
    public class ProcessedQuestionMap
    {
        public string QuestionKey { get; set; } = null!;
        public string QuestionHeader { get; set; } = null!;
        public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    }
}
