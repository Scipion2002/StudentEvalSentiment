using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Survey;

namespace StudentEvalSentiment.DB.Config
{
    public class SurveyQuestionHeaderConfig : IEntityTypeConfiguration<Models.Entities.Survey.SurveyQuestionHeader>
    {
        public void Configure(EntityTypeBuilder<Models.Entities.Survey.SurveyQuestionHeader> b)
        {
            b.HasKey(x => x.SurveyQuestionHeaderId);

            b.Property(x => x.HeaderText)
                .HasMaxLength(800)
                .IsRequired();

            b.HasIndex(x => new { x.SurveyId, x.HeaderText })
                .IsUnique();
        }
    }
}
