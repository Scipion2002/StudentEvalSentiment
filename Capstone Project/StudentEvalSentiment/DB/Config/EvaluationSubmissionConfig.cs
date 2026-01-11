using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Evaluations;

namespace StudentEvalSentiment.DB.Config
{
    public class EvaluationSubmissionConfig : IEntityTypeConfiguration<EvaluationSubmission>
    {
        public void Configure(EntityTypeBuilder<EvaluationSubmission> b)
        {
            b.HasKey(x => x.SubmissionId);

            // Optional: good practice
            b.Property(x => x.SubmittedAt).IsRequired();
            b.Property(x => x.ExternalKey).HasMaxLength(120).IsRequired(false);
        }
    }
}
