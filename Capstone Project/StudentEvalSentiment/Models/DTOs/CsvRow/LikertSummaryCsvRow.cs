namespace StudentEvalSentiment.Models.DTOs.CsvRow
{
    public class LikertSummaryCsvRow
    {
        public string TargetType { get; set; } = "";
        public string InstructorName { get; set; } = "";
        public string CourseNumber { get; set; } = "";
        public string CourseName { get; set; } = "";

        public decimal LikertAvg { get; set; }
        public int LikertCountUsed { get; set; }
        public string LabelDerived { get; set; } = "";

        public decimal? Dim1Avg { get; set; }
        public decimal? Dim2Avg { get; set; }
        public decimal? Dim3Avg { get; set; }
        public decimal? Dim4Avg { get; set; }
        public decimal? Dim5Avg { get; set; }

    }
}
