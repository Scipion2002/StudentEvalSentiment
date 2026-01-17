using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Evaluations;

namespace StudentEvalSentiment.DB.Config
{
    public class EvaluationCommentConfig : IEntityTypeConfiguration<EvaluationComment>
    {
        public void Configure(EntityTypeBuilder<EvaluationComment> b)
        {
            b.HasKey(x => x.CommentId);

            b.Property(x => x.TextAnonymized)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.TextClean)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            b.HasIndex(x => x.SectionId);
            b.HasIndex(x => x.InstructorId);
            b.HasIndex(x => x.TargetType);

            // Relationships (keep deletes safe)
            b.HasOne(x => x.Section)
                .WithMany()
                .HasForeignKey(x => x.SectionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Instructor)
                .WithMany()
                .HasForeignKey(x => x.InstructorId)
                .OnDelete(DeleteBehavior.NoAction);

            b.HasOne(x => x.TextResponse)
                .WithMany()
                .HasForeignKey(x => x.TextResponseId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
