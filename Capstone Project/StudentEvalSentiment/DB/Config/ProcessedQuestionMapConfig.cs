using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEvalSentiment.Models.Entities.Staging;

namespace StudentEvalSentiment.DB.Config
{
    public class ProcessedQuestionMapConfig : IEntityTypeConfiguration<ProcessedQuestionMap>
    {
        public void Configure(EntityTypeBuilder<ProcessedQuestionMap> b)
        {
            b.HasKey(x => x.QuestionKey);

            b.Property(x => x.QuestionKey)
                .HasMaxLength(32)
                .IsRequired();

            b.Property(x => x.QuestionHeader)
                .HasMaxLength(800) // headers are long, but not nvarchar(max)
                .IsRequired();

            b.Property(x => x.FirstSeenUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            b.Property(x => x.LastSeenUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");
        }
    }
}
