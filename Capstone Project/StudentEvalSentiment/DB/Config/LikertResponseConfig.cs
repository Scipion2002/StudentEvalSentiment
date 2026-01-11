using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Survey;

namespace StudentEvalSentiment.DB.Config
{
    public class LikertResponseConfig : IEntityTypeConfiguration<Models.Entities.Evaluations.LikertResponse>
    {
        public void Configure(EntityTypeBuilder<Models.Entities.Evaluations.LikertResponse> b)
        {
            b.HasKey(x => new { x.TargetId, x.QuestionId });

            b.Property(x => x.Value)
                .IsRequired(false);

            b.Property(x => x.IsApplicable)
                .HasDefaultValue(true);

            // --- relationships (recommended) ---
            b.HasOne(x => x.Target)
                .WithMany(t => t.LikertResponses)
                .HasForeignKey(x => x.TargetId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
