using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;
using StudentEvalSentiment.Models.DTOs.Sentiment;
using StudentEvalSentiment.Models.Ml.Sentiment;

namespace StudentEvalSentiment.Controllers
{
    [ApiController]
    [Route("api/sentiment")]
    public class SentimentController : ControllerBase
    {
        private readonly PredictionEnginePool<SentimentModelInput, SentimentModelOutput> _pool;

        public SentimentController(PredictionEnginePool<SentimentModelInput, SentimentModelOutput> pool)
        {
            _pool = pool;
        }

        [HttpPost("predict")]
        public ActionResult<SentimentPredictResponse> Predict([FromBody] SentimentPredictRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Text))
                return BadRequest("Text is required.");

            // If you want: clean text here (later)
            var pred = _pool.Predict("SentimentModel", new SentimentModelInput
            {
                TextClean = req.Text,
                Label = string.Empty // dummy
            });


            return Ok(new SentimentPredictResponse(pred.PredictedLabel));
        }
    }
}
