using StudentEvalSentiment.Models.DTOs.Common;

namespace StudentEvalSentiment.Models.DTOs.Topics
{
    public sealed record TopicPredictRequest(
    TargetTypeDto TargetType,
    string Text
);
}
