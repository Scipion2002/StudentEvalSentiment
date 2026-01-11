using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Survey;

namespace StudentEvalSentiment.DB.Config
{
    public class SurveyQuestionHeaderConfig : IEntityTypeConfiguration<SurveyQuestionHeader>
    {
        public void Configure(EntityTypeBuilder<SurveyQuestionHeader> b)
        {
            b.HasKey(x => x.SurveyQuestionHeaderId);

            b.Property(x => x.HeaderText)
                .HasMaxLength(800)
                .IsRequired();

            b.HasIndex(x => new { x.SurveyId, x.HeaderText })
                .IsUnique();

            // Keep this cascade (fine):
            b.HasOne(x => x.SurveyQuestion)
                .WithMany()
                .HasForeignKey(x => x.SurveyQuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Change THIS to Restrict/NoAction to avoid multiple cascade paths:
            b.HasOne(x => x.Survey)
                .WithMany()
                .HasForeignKey(x => x.SurveyId)
                .OnDelete(DeleteBehavior.NoAction); // or DeleteBehavior.Restrict
        }
    }
}
