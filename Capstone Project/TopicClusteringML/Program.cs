using Microsoft.ML;
using Microsoft.ML.Data;
using MLTrainer.Models.Training;
using System.Text;

var ml = new MLContext(seed: 42);

string dataBasePath = @"C:\Users\alexh\OneDrive - Neumont College of Computer Science\Documents\Masters\PRO590\Capstone Project\StudentEvalSentiment\Python\Datasets";

TrainOne(
    Path.Combine(dataBasePath, "topic_instructor.csv"),
    "topic_model_instructor.zip",
    "topic_validation_instructor.csv",
    k: 90);

TrainOne(
    Path.Combine(dataBasePath, "topic_course.csv"),
    "topic_model_course.zip",
    "topic_validation_course.csv",
    k: 90);


void TrainOne(string dataPath, string modelPath, string validationOutPath, int k)
{
    Console.WriteLine($"\n=== Training: {dataPath} (k={k}) ===");

    var data = ml.Data.LoadFromTextFile<TopicInput>(
        path: dataPath,
        hasHeader: true,
        separatorChar: ',',
        allowQuoting: true);

    // Text -> Features -> KMeans
    var pipeline =
    ml.Transforms.CopyColumns("TextClean", nameof(TopicInput.TextClean))
    .Append(ml.Transforms.Text.FeaturizeText("Features", "TextClean"))
    .Append(ml.Clustering.Trainers.KMeans("Features", numberOfClusters: k));

    var model = pipeline.Fit(data);

    // Evaluate
    var transformed = model.Transform(data);
    var metrics = ml.Clustering.Evaluate(transformed, scoreColumnName: "Score", featureColumnName: "Features");

    Console.WriteLine($"AverageDistance: {metrics.AverageDistance:0.###}");
    Console.WriteLine($"DaviesBouldinIndex: {metrics.DaviesBouldinIndex:0.###} (lower is better)");

    // Save model
    ml.Model.Save(model, data.Schema, modelPath);
    Console.WriteLine($"Saved model: {modelPath}");

    // Validation artifact: samples per cluster
    WriteClusterValidationCsv(transformed, validationOutPath, k);
    Console.WriteLine($"Wrote validation file: {validationOutPath}");
}

void WriteClusterValidationCsv(IDataView transformed, string outPath, int k)
{
    // Pull just ClusterId + TextClean into memory
    var rows = ml.Data.CreateEnumerable<ClusterTextRow>(transformed, reuseRowObject: false).ToList();

    // Group by cluster
    var groups = rows.GroupBy(r => r.ClusterId)
                     .OrderBy(g => g.Key)
                     .ToList();

    // Write CSV: ClusterId,Count,Sample1,Sample2,Sample3,Sample4,Sample5
    var sb = new StringBuilder();
    sb.AppendLine("ClusterId,Count,Sample1,Sample2,Sample3,Sample4,Sample5");

    var rng = new Random(42);

    foreach (var g in groups)
    {
        var samples = g.Select(x => x.TextClean)
                       .Where(t => !string.IsNullOrWhiteSpace(t))
                       .OrderBy(_ => rng.Next())
                       .Take(5)
                       .Select(CsvEscape)
                       .ToList();

        while (samples.Count < 5) samples.Add("");

        sb.AppendLine($"{g.Key},{g.Count()},{samples[0]},{samples[1]},{samples[2]},{samples[3]},{samples[4]}");
    }

    File.WriteAllText(outPath, sb.ToString());
}

string CsvEscape(string s)
{
    s = s.Replace("\"", "\"\"");
    return $"\"{s}\"";
}
