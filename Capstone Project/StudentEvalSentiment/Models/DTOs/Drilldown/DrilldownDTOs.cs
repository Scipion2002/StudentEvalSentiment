namespace StudentEvalSentiment.Models.DTOs.Drilldown
{
    public class DrilldownDTOs
    {
        public sealed record DrilldownAnswerDto(
        long ProcessedCommentId,
        string RawText
        );

        public sealed record DrilldownQuestionDto(
            string QuestionKey,
            string QuestionHeader,
            int Count,
            IReadOnlyList<DrilldownAnswerDto> Answers
        );

        public sealed record SentimentDrilldownResponseDto(
            string Sentiment,
            int TotalAnswers,
            IReadOnlyList<DrilldownQuestionDto> Questions
        );
    }
}
