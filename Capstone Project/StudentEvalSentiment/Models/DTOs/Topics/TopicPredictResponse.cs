namespace StudentEvalSentiment.Models.DTOs.Topics
{
    public sealed record TopicPredictResponse(
    int TopicClusterId,
    string? HumanLabel = null
);
}
