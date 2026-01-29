using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MLTrainer.Models.Training
{
    public class TopicInput
    {
        [LoadColumn(0)]
        public string TextClean { get; set; } = "";
    }
}
