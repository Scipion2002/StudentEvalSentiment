using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Microsoft.OpenApi;
using StudentEvalSentiment.DB.Context;
using StudentEvalSentiment.Models.Ml.Sentiment;
using StudentEvalSentiment.Models.Ml.Topics;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Services
// --------------------


// MVC + API Controllers
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// (Optional) If you still use Views/Razor pages, keep this instead of AddControllers()
// builder.Services.AddControllersWithViews();

// Swagger (recommended for testing upload endpoint)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Student Evaluation Sentiment API", Version = "v1" });
});

// EF Core - SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// If you use CORS for Angular later
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var sentimentPath = Path.Combine(AppContext.BaseDirectory, "MLModels", "sentiment_model.zip");
var topicInstructorPath = Path.Combine(AppContext.BaseDirectory, "MLModels", "topic_model_instructor.zip");
var topicCoursePath = Path.Combine(AppContext.BaseDirectory, "MLModels", "topic_model_course.zip");

builder.Services.AddPredictionEnginePool<SentimentModelInput, SentimentModelOutput>()
    .FromFile(modelName: "SentimentModel", filePath: sentimentPath, watchForChanges: true);

builder.Services.AddPredictionEnginePool<TopicModelInput, TopicModelOutput>()
    .FromFile(modelName: "TopicInstructorModel", filePath: topicInstructorPath, watchForChanges: true);

builder.Services.AddPredictionEnginePool<TopicModelInput, TopicModelOutput>()
    .FromFile(modelName: "TopicCourseModel", filePath: topicCoursePath, watchForChanges: true);

var app = builder.Build();

// --------------------
// Middleware
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapGet("/", () => Results.Redirect("/swagger"));
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Evaluation Sentiment API v1"));
    app.UseCors("DevCors");
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.UseCors(policy =>
    policy.WithOrigins("http://localhost:4200")
          .AllowAnyHeader()
          .AllowAnyMethod());

// If you're serving Angular static files later, you can add app.UseStaticFiles();
app.MapControllers();
app.UseStaticFiles();

// If you still want MVC default route (views), uncomment:
// app.MapControllerRoute(
//     name: "default",
//     pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
