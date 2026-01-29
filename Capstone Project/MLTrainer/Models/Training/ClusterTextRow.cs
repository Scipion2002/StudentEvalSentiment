using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MLTrainer.Models.Training
{
    public class ClusterTextRow
    {
        [ColumnName("PredictedLabel")]
        public uint ClusterId { get; set; }

        public string TextClean { get; set; } = "";
    }
}
