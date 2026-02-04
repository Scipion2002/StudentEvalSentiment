using Microsoft.ML.Data;

namespace StudentEvalSentiment.Models.Ml.Topics
{
    public sealed class TopicModelOutput
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedLabel { get; set; }
    }
}
