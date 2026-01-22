using Microsoft.ML;
using Microsoft.ML.Data;
using MLTrainer.Models.Training;

var ml = new MLContext(seed: 42);

// Load CSV
// sentiment_training.csv must have headers: Label,TextClean
var data = ml.Data.LoadFromTextFile<SentimentInput>(
    path: "sentiment_training.csv",
    hasHeader: true,
    separatorChar: ',');

// Split
var split = ml.Data.TrainTestSplit(data, testFraction: 0.2);

// Pipeline
var pipeline =
    ml.Transforms.Conversion.MapValueToKey("Label", "Label")
    .Append(ml.Transforms.Text.FeaturizeText("Features", "TextClean"))
    .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy())
    .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

// Train
var model = pipeline.Fit(split.TrainSet);

// Evaluate
var predictions = model.Transform(split.TestSet);
var metrics = ml.MulticlassClassification.Evaluate(predictions);

Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy:0.###}");
Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy:0.###}");
Console.WriteLine($"LogLoss: {metrics.LogLoss:0.###}");

// Confusion matrix -> per-class precision/recall/F1
PrintPerClassMetrics(metrics.ConfusionMatrix);

// Save model
ml.Model.Save(model, split.TrainSet.Schema, "sentiment_model.zip");
Console.WriteLine("Saved model to sentiment_model.zip");

static void PrintPerClassMetrics(ConfusionMatrix cm)
{
    var n = cm.NumberOfClasses;
    var counts = cm.Counts; // [actual][predicted]

    Console.WriteLine("\nPer-class Precision/Recall/F1:");
    for (int c = 0; c < n; c++)
    {
        double tp = counts[c][c];
        double fp = 0;
        double fn = 0;

        for (int i = 0; i < n; i++)
        {
            if (i != c) fp += counts[i][c]; // predicted c but actual i
            if (i != c) fn += counts[c][i]; // actual c but predicted i
        }

        double precision = tp + fp == 0 ? 0 : tp / (tp + fp);
        double recall = tp + fn == 0 ? 0 : tp / (tp + fn);
        double f1 = (precision + recall) == 0 ? 0 : 2 * precision * recall / (precision + recall);

        Console.WriteLine($"Class {c}: P={precision:0.###} R={recall:0.###} F1={f1:0.###}");
    }
}
