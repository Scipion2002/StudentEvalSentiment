namespace StudentEvalSentiment.Models.Entities.Survey
{
    public class Survey
    {
        public int SurveyId { get; set; }
        public string Name { get; set; } = null!;           // surveyname
        public DateTime? DateStart { get; set; }            // datestart
        public DateTime? DateClose { get; set; }            // dateclose
        public string? Version { get; set; }
    }
}
