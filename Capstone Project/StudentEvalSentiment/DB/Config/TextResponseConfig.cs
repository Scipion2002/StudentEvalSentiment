using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Evaluations;

namespace StudentEvalSentiment.DB.Config
{
    public class TextResponseConfig : IEntityTypeConfiguration<TextResponse>
    {
        public void Configure(EntityTypeBuilder<TextResponse> b)
        {
            b.HasKey(x => x.TextResponseId);

            b.Property(x => x.RawText)
                .IsRequired(false);

            b.Property(x => x.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Helpful index for queries like "all comments for instructor/course target"
            b.HasIndex(x => x.TargetId);

            // Relationships
            b.HasOne(x => x.Target)
                .WithMany(t => t.TextResponses)   // change to .WithMany() if you don't have this collection
                .HasForeignKey(x => x.TargetId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
