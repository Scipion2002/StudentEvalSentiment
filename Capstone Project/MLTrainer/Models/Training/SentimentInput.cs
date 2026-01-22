using System;
using System.Collections.Generic;
using System.Text;

namespace MLTrainer.Models.Training
{
    public class SentimentInput
    {
        public string TextClean { get; set; } = "";
        public string Label { get; set; } = ""; // "Positive" / "Neutral" / "Negative"
    }
}
