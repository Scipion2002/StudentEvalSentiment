using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Evaluations;

namespace StudentEvalSentiment.DB.Config
{
    public class EvaluationTargetConfig : IEntityTypeConfiguration<Models.Entities.Evaluations.EvaluationTarget>
    {
        public void Configure(EntityTypeBuilder<Models.Entities.Evaluations.EvaluationTarget> b)
        {
            b.HasKey(x => x.TargetId);
            b.HasIndex(x => new { x.SubmissionId, x.TargetType, x.TargetOrder })
                .IsUnique();
        }
    }
}
