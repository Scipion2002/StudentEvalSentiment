namespace StudentEvalSentiment.Models.Ml.Topics
{
    public sealed class TopicModelInput
    {
        public string TextClean { get; set; } = "";
        // Required because your trained pipeline includes MapValueToKey on "Label"
        public string Label { get; set; } = "";
    }
}
