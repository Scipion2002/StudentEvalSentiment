using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Survey;

namespace StudentEvalSentiment.DB.Config
{
    public class SurveyQuestionConfig : IEntityTypeConfiguration<Models.Entities.Survey.SurveyQuestion>
    {
        public void Configure(EntityTypeBuilder<Models.Entities.Survey.SurveyQuestion> b)
        {
            b.HasKey(x => x.SurveyQuestionId);

            b.Property(x => x.QuestionKey)
                .HasMaxLength(120)
                .IsRequired();

            b.Property(x => x.CanonicalText)
                .HasMaxLength(600)
                .IsRequired();

            b.HasIndex(x => new { x.SurveyId, x.QuestionKey })
                .IsUnique();
        }
    }
}
