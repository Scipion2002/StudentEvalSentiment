using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MLTrainer.Models.Training
{
    public class SentimentInput
    {
        [LoadColumn(7)]
        public string TextClean { get; set; } = "";
        [LoadColumn(8)]
        public string Label { get; set; } = ""; // "Positive" / "Neutral" / "Negative"
    }
}
