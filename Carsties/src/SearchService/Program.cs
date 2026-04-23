using System.Net;
using Polly;
using Polly.Extensions.Http;
using SearchService.Data;
using SearchService.Services;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register the AuctionSvcHttpClient with an HTTP client factory and attach the defined Polly policy for resilience.
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());

builder.Services.AddMassTransit(x =>
{
	// Register consumers here
	x.UsingRabbitMq((context, cfg) =>
	{
		// Configure RabbitMQ settings here
		cfg.ConfigureEndpoints(context);
	});
});

// build the app
var app = builder.Build();

app.UseAuthorization();

// Map controllers to handle incoming requests
app.MapControllers();

// Register a callback to initialize the database when the application starts. 
// This ensures that the database is set up and populated with data from the Auction Service API before handling any requests.
// This way the application can server requests immediately after startup without waiting for the database initialization to complete.
app.Lifetime.ApplicationStarted.Register(async () =>
{

	try
	{
		await DbInitializer.InitDb(app);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error initializing database: {ex.Message}");
	}

});

app.Run();

// Define a Polly policy to handle transient HTTP errors and 404 Not Found responses,
// with an infinite retry mechanism that waits for 2 seconds between retries.
static IAsyncPolicy<HttpResponseMessage> GetPolicy() => HttpPolicyExtensions.HandleTransientHttpError()
																																						.OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
																																						.WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(2));