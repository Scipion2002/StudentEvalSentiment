using Microsoft.AspNetCore.Mvc;

namespace StudentEvalSentiment.Models.Requests
{
    public class CourseEvalUploadRequest
    {
        [FromForm(Name = "file")]
        public IFormFile File { get; set; } = null!;
    }
}
