using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopicAssigner.Data
{
    public class TopicPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint ClusterId { get; set; }  // ML.NET returns uint
    }
}
