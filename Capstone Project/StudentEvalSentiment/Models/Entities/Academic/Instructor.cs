namespace StudentEvalSentiment.Models.Entities.Academic
{
    public class Instructor
    {
        public int InstructorId { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? Email { get; set; }
    }
}
