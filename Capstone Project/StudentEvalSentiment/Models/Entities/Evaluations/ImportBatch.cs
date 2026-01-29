namespace StudentEvalSentiment.Models.Entities.Evaluations
{
    public class ImportBatch
    {
        public Guid ImportBatchId { get; set; }
        public string SourceFileName { get; set; } = "";
        public string FileHashSha256 { get; set; } = ""; // 64 hex chars
        public long FileSizeBytes { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
