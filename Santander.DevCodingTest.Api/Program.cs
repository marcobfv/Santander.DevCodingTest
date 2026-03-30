
using Santander.DevCodingTest.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<IHackerNewsService, HackerNewsService>(client =>
{
    var baseUrl = builder.Configuration["HackerNews:BaseUrl"]!;
    var timeout = int.Parse(builder.Configuration["HackerNews:TimeoutSeconds"]!);

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeout);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Santander - Dev Coding Test API",
        Version = "v1",
        Description = "Returns the best N stories from Hacker News ordered by score descending."
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Santander - Dev Coding Test API v1");
        options.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program { }