namespace StudentEvalSentiment.Models.Entities.Staging
{
    public class ProcessedComment
    {
        public long ProcessedCommentId { get; set; }

        public Guid ImportBatchId { get; set; }                 // groups one upload
        public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;

        public string SourceFileName { get; set; } = null!;

        public string TargetType { get; set; } = null!;         // "Instructor" or "Course"
        public string InstructorName { get; set; } = "";        // from crs_dir
        public string CourseNumber { get; set; } = "";          // crsnum
        public string CourseName { get; set; } = "";            // crsname

        public string? RawText { get; set; }                    // optional (you can drop later)
        public string TextClean { get; set; } = null!;          // cleaned text for ML
    }
}
