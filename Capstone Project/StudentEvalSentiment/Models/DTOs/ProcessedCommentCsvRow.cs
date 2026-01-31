namespace StudentEvalSentiment.Models.DTOs
{
    public class ProcessedCommentCsvRow
    {
        public string TargetType { get; set; } = "";
        public string InstructorName { get; set; } = "";
        public string CourseNumber { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string SentimentLabel { get; set; } = ""; // Sentiment label

        public string QuestionKey { get; set; } = "";
        public string QuestionHeader { get; set; } = ""; // only for mapping

        public string? RawText { get; set; }
        public string TextClean { get; set; } = "";
    }
}
