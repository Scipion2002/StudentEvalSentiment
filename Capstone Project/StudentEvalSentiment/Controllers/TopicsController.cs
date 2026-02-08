using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;
using StudentEvalSentiment.Models.DTOs.Common;
using StudentEvalSentiment.Models.DTOs.Topics;
using StudentEvalSentiment.Models.Ml.Topics;

namespace StudentEvalSentiment.Controllers
{
    [ApiController]
    [Route("api/topics")]
    public class TopicsController : ControllerBase
    {
        private readonly PredictionEnginePool<TopicModelInput, TopicModelOutput> _pool;

        public TopicsController(PredictionEnginePool<TopicModelInput, TopicModelOutput> pool)
        {
            _pool = pool;
        }

        [HttpPost("predict")]
        public ActionResult<TopicPredictResponse> Predict([FromBody] TopicPredictRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Text))
                return BadRequest("Text is required.");

            var modelName = req.TargetType switch
            {
                TargetTypeDto.Instructor => "TopicInstructorModel",
                TargetTypeDto.Course => "TopicCourseModel",
                _ => null
            };

            if (modelName == null)
                return BadRequest("TargetType must be Instructor or Course.");

            var pred = _pool.Predict(modelName, new TopicModelInput
            {
                TextClean = req.Text,
                Label = string.Empty
            });

            return Ok(new TopicPredictResponse((int)pred.ClusterId, null));
        }
    }
}
