using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MLTrainer.Models.Training
{
    public class TopicPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint ClusterId { get; set; }

        public float[] Score { get; set; } = Array.Empty<float>();
    }
}
