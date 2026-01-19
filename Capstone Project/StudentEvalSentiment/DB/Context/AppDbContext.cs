using Microsoft.EntityFrameworkCore;
using StudentEvalSentiment.Models.Entities.Evaluations;
using StudentEvalSentiment.Models.Entities.Staging;
using StudentEvalSentiment.Models.Entities.Survey;

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

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Automatically apply all IEntityTypeConfiguration<> in this assembly
            b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
