namespace StudentEvalSentiment.Models.Entities.Academic
{
    public class Course
    {
        public int CourseId { get; set; }
        public string CourseNumber { get; set; } = null!;   // crsnum
        public string CourseName { get; set; } = null!;     // crsname
        public string? DepartmentName { get; set; }         // deptname
    }
}
