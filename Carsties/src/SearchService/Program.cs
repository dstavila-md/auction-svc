using SearchService.Data;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddHttpClient<AuctionSvcHttpClient>();

// build the app
var app = builder.Build();

app.UseAuthorization();

// Map controllers to handle incoming requests
app.MapControllers();

try
{
	await DbInitializer.InitDb(app);
}
catch (Exception ex)
{
	Console.WriteLine($"Error initializing database: {ex.Message}");
}

app.Run();