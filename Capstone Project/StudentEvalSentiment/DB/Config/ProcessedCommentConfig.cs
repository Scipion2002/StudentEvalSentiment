using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Staging;

namespace StudentEvalSentiment.DB.Config
{
    public class ProcessedCommentConfig : IEntityTypeConfiguration<ProcessedComment>
    {
        public void Configure(EntityTypeBuilder<ProcessedComment> b)
        {
            b.HasKey(x => x.ProcessedCommentId);

            b.Property(x => x.SourceFileName).HasMaxLength(260).IsRequired();
            b.Property(x => x.TargetType).HasMaxLength(20).IsRequired();

            b.Property(x => x.InstructorName).HasMaxLength(200);
            b.Property(x => x.CourseNumber).HasMaxLength(50);
            b.Property(x => x.CourseName).HasMaxLength(200);

            b.Property(x => x.TextClean).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.RawText).HasColumnType("nvarchar(max)").IsRequired(false);
            b.Property(x => x.QuestionKey).HasMaxLength(32).IsRequired();
            b.Property(x => x.TopicModel).HasMaxLength(20);
            b.HasIndex(x => new { x.ImportBatchId, x.TargetType, x.TopicClusterId });
            b.Property(x => x.SentimentLabel).HasMaxLength(20);
            b.HasIndex(x => new { x.ImportBatchId, x.TargetType, x.SentimentLabel });

            b.HasIndex(x => x.ImportBatchId);
            b.HasIndex(x => x.CourseNumber);
            b.HasIndex(x => x.InstructorName);
            b.HasIndex(x => x.TargetType);
            b.HasIndex(x => new { x.ImportBatchId, x.QuestionKey });
        }
    }
}
