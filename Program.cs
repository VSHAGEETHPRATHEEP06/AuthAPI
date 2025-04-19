var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure MongoDB settings
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try {
    Console.WriteLine("Testing MongoDB connection...");
    var connectionString = builder.Configuration["MongoDbSettings:ConnectionString"];
    var databaseName = builder.Configuration["MongoDbSettings:DatabaseName"]; 
    
    Console.WriteLine($"Connection string: {connectionString}");
    Console.WriteLine($"Database name: {databaseName}");
    
    if (!string.IsNullOrEmpty(connectionString)) {
        var client = new MongoDB.Driver.MongoClient(connectionString);
        var database = client.GetDatabase(databaseName ?? "test");
        Console.WriteLine("MongoDB connection successful!");
    } else {
        Console.WriteLine("ERROR: Connection string is empty!");
    }
    } catch (Exception ex) {
        Console.WriteLine($"MongoDB connection error: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
