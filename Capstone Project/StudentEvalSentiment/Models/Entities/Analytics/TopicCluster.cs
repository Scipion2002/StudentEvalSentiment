namespace StudentEvalSentiment.Models.Entities.Analytics
{
    public class TopicCluster
    {
        public int TopicClusterId { get; set; }
        public string TargetType { get; set; } = "";   // "Instructor" or "Course"
        public string HumanLabel { get; set; } = "";
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
