using Microsoft.EntityFrameworkCore;
using StudentEvalSentiment.Models.Entities.Analytics;
using StudentEvalSentiment.Models.Entities.Evaluations;
using StudentEvalSentiment.Models.Entities.Staging;
using StudentEvalSentiment.Models.Entities.Survey;
using System.Reflection.Emit;

namespace StudentEvalSentiment.DB.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<EvaluationSubmission> EvaluationSubmissions => Set<EvaluationSubmission>();
        public DbSet<EvaluationTarget> EvaluationTargets => Set<EvaluationTarget>();
        public DbSet<LikertResponse> LikertResponses => Set<LikertResponse>();
        public DbSet<TextResponse> TextResponses => Set<TextResponse>();
        public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
        public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
        public DbSet<SurveyQuestionHeader> SurveyQuestionHeaders => Set<SurveyQuestionHeader>();
        public DbSet<EvaluationComment> EvaluationComments => Set<EvaluationComment>();
        public DbSet<ProcessedComment> ProcessedComments => Set<ProcessedComment>();
        public DbSet<ProcessedQuestionMap> ProcessedQuestionMaps => Set<ProcessedQuestionMap>();
        public DbSet<ProcessedLikertSummary> ProcessedLikertSummaries => Set<ProcessedLikertSummary>();
        public DbSet<TopicCluster> TopicClusters => Set<TopicCluster>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<TopicCluster>(b =>
            {
                b.ToTable("TopicClusters");
                b.HasKey(x => x.TopicClusterId);

                b.Property(x => x.TargetType).HasMaxLength(50).IsRequired();
                b.Property(x => x.HumanLabel).HasMaxLength(200).IsRequired();
                b.Property(x => x.Notes).HasMaxLength(2000);
                b.Property(x => x.CreatedUtc).IsRequired();
            });

            // Automatically apply all IEntityTypeConfiguration<> in this assembly
            b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
