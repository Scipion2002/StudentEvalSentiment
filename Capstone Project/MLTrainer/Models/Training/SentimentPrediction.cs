using System;
using System.Collections.Generic;
using System.Text;

namespace MLTrainer.Models.Training
{
    public class SentimentPrediction
    {
        public string PredictedLabel { get; set; } = "";
        public float[] Score { get; set; } = Array.Empty<float>();
    }
}
