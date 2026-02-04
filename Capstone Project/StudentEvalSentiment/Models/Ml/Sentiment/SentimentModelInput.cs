namespace StudentEvalSentiment.Models.Ml.Sentiment
{
    public sealed class SentimentModelInput
    {
        public string TextClean { get; set; } = "";
        // Required because your trained pipeline includes MapValueToKey on "Label"
        public string Label { get; set; } = "";
    }
}
