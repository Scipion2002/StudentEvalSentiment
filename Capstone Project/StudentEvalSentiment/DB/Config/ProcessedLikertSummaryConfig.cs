using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Evaluations;

namespace StudentEvalSentiment.DB.Config
{
    public class ProcessedLikertSummaryConfig : IEntityTypeConfiguration<ProcessedLikertSummary>
    {
        public void Configure(EntityTypeBuilder<ProcessedLikertSummary> b)
        {
            b.HasKey(x => x.ProcessedLikertSummaryId);

            b.Property(x => x.TargetType).HasMaxLength(20).IsRequired();
            b.Property(x => x.InstructorName).HasMaxLength(200);
            b.Property(x => x.CourseNumber).HasMaxLength(50);
            b.Property(x => x.CourseName).HasMaxLength(300);

            b.Property(x => x.LabelDerived).HasMaxLength(20);

            b.HasIndex(x => new
            {
                x.ImportBatchId,
                x.TargetType,
                x.InstructorName,
                x.CourseNumber
            });
        }
    }
}
