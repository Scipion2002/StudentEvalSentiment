namespace StudentEvalSentiment.Models.Insights;

public sealed record InsightFiltersResponseDto(
    IReadOnlyList<Guid> ImportBatches,
    IReadOnlyList<string> Instructors,
    IReadOnlyList<CourseOptionDto> Courses,
    IReadOnlyList<TopicOptionDto> Topics
);

public sealed record CourseOptionDto(string CourseNumber, string CourseName);
public sealed record TopicOptionDto(int TopicClusterId, string HumanLabel);

public sealed record InsightsResponseDto(
    SentimentBreakdownDto Sentiment,
    IReadOnlyList<TopicCountDto> TopTopics
);

public sealed record SentimentBreakdownDto(int Positive, int Neutral, int Negative)
{
    public int Total => Positive + Neutral + Negative;
}

public sealed record TopicCountDto(int TopicClusterId, string HumanLabel, int Count);

public sealed class InsightQuery
{
    public Guid? ImportBatchId { get; init; }     // trimester
    public string TargetType { get; init; } = "Instructor"; // "Instructor" | "Course"

    public string? InstructorName { get; init; } // for Instructor insights
    public string? CourseNumber { get; init; }   // for Course insights
    public int? TopicClusterId { get; init; }    // optional drilldown
}
