using Microsoft.ML.Data;

namespace StudentEvalSentiment.Models.Ml.Sentiment
{
    public sealed class SentimentModelOutput
    {
        // Your trainer maps PredictedLabel back to string
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = "";
    }
}
