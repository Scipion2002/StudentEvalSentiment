using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using StudentEvalSentiment.DB.Context;
using TopicAssigner.Data;

// Load configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var ml = new MLContext(seed: 42);

// TODO: set these paths
var instructorModelPath = @"C:\Users\alexh\OneDrive - Neumont College of Computer Science\Documents\Masters\PRO590\Capstone Project\TopicClusteringML\bin\Debug\net10.0\topic_model_instructor.zip";
var courseModelPath = @"C:\Users\alexh\OneDrive - Neumont College of Computer Science\Documents\Masters\PRO590\Capstone Project\TopicClusteringML\bin\Debug\net10.0\topic_model_course.zip";

// TODO: build DbContext (same connection string as API)
var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
var db = new AppDbContext(optionsBuilder.Options);

await AssignForTargetAsync(db, ml, "Instructor", instructorModelPath);
await AssignForTargetAsync(db, ml, "Course", courseModelPath);

Console.WriteLine("Done.");

static async Task AssignForTargetAsync(AppDbContext db, MLContext ml, string targetType, string modelPath)
{
    Console.WriteLine($"Assigning topics for {targetType} using {modelPath}");

    // Load model + prediction engine
    var model = ml.Model.Load(modelPath, out _);
    var predEngine = ml.Model.CreatePredictionEngine<TopicInput, TopicPrediction>(model);

    // Pull comments that need topic assignment
    // Keep this batchy so it doesn’t load everything at once
    const int batchSize = 2000;
    var now = DateTime.UtcNow;

    int updatedTotal = 0;
    while (true)
    {
        var batch = await db.ProcessedComments
            .Where(c => c.TargetType == targetType && c.TopicClusterId == null && c.TextClean != null && c.TextClean != "")
            .OrderBy(c => c.ProcessedCommentId)
            .Take(batchSize)
            .ToListAsync();

        if (batch.Count == 0) break;

        foreach (var c in batch)
        {
            var pred = predEngine.Predict(new TopicInput { TextClean = c.TextClean! });

            // KMeans clusters are 1..k (uint). Store as int.
            c.TopicClusterId = (int)pred.ClusterId;
            c.TopicModel = targetType;
            c.TopicAssignedUtc = now;
        }

        updatedTotal += batch.Count;
        await db.SaveChangesAsync();

        Console.WriteLine($"  Updated {updatedTotal} so far...");
    }

    Console.WriteLine($"Finished {targetType}. Updated total: {updatedTotal}");
}
