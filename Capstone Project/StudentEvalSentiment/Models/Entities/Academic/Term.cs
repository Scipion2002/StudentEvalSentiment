namespace StudentEvalSentiment.Models.Entities.Academic
{
    public class Term
    {
        public int TermId { get; set; }
        public string Name { get; set; } = null!;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
