using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Tests
{
    public class EndpointsSmokeTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;

        public EndpointsSmokeTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Sentiment_Predict_Returns_Label()
        {
            var res = await _client.PostAsJsonAsync("/api/sentiment/predict", new { text = "Great instructor, very helpful." });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var body = await res.Content.ReadFromJsonAsync<SentimentResponse>();
            Assert.NotNull(body);
            Assert.False(string.IsNullOrWhiteSpace(body!.Label));
        }

        [Fact]
        public async Task Topics_Predict_Instructor_Returns_ClusterId()
        {
            var res = await _client.PostAsJsonAsync("/api/topics/predict",
                new { targetType = "Instructor", text = "Responds quickly and gives feedback." });

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var body = await res.Content.ReadFromJsonAsync<TopicResponse>();
            Assert.NotNull(body);
            Assert.True(body!.TopicClusterId > 0);
        }

        [Fact]
        public async Task Topics_Predict_Course_Returns_ClusterId()
        {
            var res = await _client.PostAsJsonAsync("/api/topics/predict",
                new { targetType = "Course", text = "Good amount of homework" });

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var body = await res.Content.ReadFromJsonAsync<TopicResponse>();
            Assert.NotNull(body);
            Assert.True(body!.TopicClusterId > 0);
        }

        [Fact]
        public async Task Sentiment_Predict_EmptyText_Returns_400()
        {
            var res = await _client.PostAsJsonAsync("/api/sentiment/predict", new { text = "" });
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        private sealed class SentimentResponse
        {
            public string Label { get; set; } = "";
            public float? Confidence { get; set; }
        }

        private sealed class TopicResponse
        {
            public int TopicClusterId { get; set; }
            public string? HumanLabel { get; set; }
        }
    }
}
