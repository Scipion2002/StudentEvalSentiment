namespace StudentEvalSentiment.Models.Entities.Academic
{
    public class Section
    {
        public int SectionId { get; set; }
        public int CourseId { get; set; }
        public int TermId { get; set; }

        public string? SectionCode { get; set; }            // if you have one
        public Course Course { get; set; } = null!;
        public Term Term { get; set; } = null!;
    }
}
