namespace StudentEvalSentiment.Models.Entities.Evaluations
{
    public class ImportBatch
    {
        public Guid ImportBatchId { get; set; } = Guid.NewGuid();
        public string SourceFileName { get; set; } = null!;
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public int RowCountTotal { get; set; }
        public int RowCountImported { get; set; }
        public string? Notes { get; set; }
    }
}
