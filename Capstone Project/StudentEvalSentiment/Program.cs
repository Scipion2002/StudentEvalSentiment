using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using StudentEvalSentiment.DB.Context;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Services
// --------------------

// MVC + API Controllers
builder.Services.AddControllers();

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

// If you're serving Angular static files later, you can add app.UseStaticFiles();
app.MapControllers();

// If you still want MVC default route (views), uncomment:
// app.MapControllerRoute(
//     name: "default",
//     pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
