namespace StudentEvalSentiment.Models.Entities.Evaluations
{
    public class ProcessedLikertSummary
    {
        public long ProcessedLikertSummaryId { get; set; }

        public Guid ImportBatchId { get; set; }

        public string TargetType { get; set; } = ""; // Instructor | Course

        public string? InstructorName { get; set; }

        public string CourseNumber { get; set; } = "";
        public string CourseName { get; set; } = "";

        // Overall
        public decimal LikertAvg { get; set; }
        public int LikertCountUsed { get; set; }
        public string LabelDerived { get; set; } = ""; // Positive/Neutral/Negative

        // Dimension averages (5)
        public decimal? Dim1Avg { get; set; }
        public decimal? Dim2Avg { get; set; }
        public decimal? Dim3Avg { get; set; }
        public decimal? Dim4Avg { get; set; }
        public decimal? Dim5Avg { get; set; }


        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
