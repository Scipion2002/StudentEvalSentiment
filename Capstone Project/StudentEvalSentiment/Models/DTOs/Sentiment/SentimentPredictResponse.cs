namespace StudentEvalSentiment.Models.DTOs.Sentiment
{
    public sealed record SentimentPredictResponse(
    string Label,
    float? Confidence = null
);
}
