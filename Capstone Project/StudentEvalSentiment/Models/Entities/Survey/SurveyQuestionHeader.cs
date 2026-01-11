namespace StudentEvalSentiment.Models.Entities.Survey
{
    public class SurveyQuestionHeader
    {
        public int SurveyQuestionHeaderId { get; set; }

        public int SurveyId { get; set; }
        public Survey Survey { get; set; } = null!;

        // EXACT CSV column header (unique in your file)
        public string HeaderText { get; set; } = null!;

        public int SurveyQuestionId { get; set; }
        public SurveyQuestion SurveyQuestion { get; set; } = null!;

        public TargetType TargetType { get; set; }
        public int? TargetOrder { get; set; }    // Instructor #1/#2... ; Course usually 1

        public bool IsActive { get; set; } = true;
    }
}
