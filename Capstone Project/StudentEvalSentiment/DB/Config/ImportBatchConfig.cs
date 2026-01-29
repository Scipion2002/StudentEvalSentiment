using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Evaluations;

namespace StudentEvalSentiment.DB.Config
{
    public class ImportBatchConfig : IEntityTypeConfiguration<ImportBatch>
    {
        public void Configure(EntityTypeBuilder<ImportBatch> b)
        {
            b.HasKey(x => x.ImportBatchId);

            b.Property(x => x.SourceFileName).HasMaxLength(260).IsRequired();
            b.Property(x => x.FileHashSha256).HasMaxLength(64).IsRequired();

            b.HasIndex(x => x.FileHashSha256).IsUnique();
        }
    }
}
