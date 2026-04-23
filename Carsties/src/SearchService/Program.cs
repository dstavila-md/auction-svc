using System.Net;
using Polly;
using Polly.Extensions.Http;
using SearchService.Data;
using SearchService.Services;
using MassTransit;
using SearchService.Consumers;
using Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Register the AuctionSvcHttpClient with an HTTP client factory and attach the defined Polly policy for resilience.
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());

builder.Services.AddMassTransit(x =>
{
	// Register consumers here
	// This will automatically register all consumers in the same assembly as AuctionCreatedConsumer
	x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
	// Configure the endpoint name formatter to use kebab-case for better readability in RabbitMQ
	x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));

	x.UsingRabbitMq((context, cfg) =>
	{
		cfg.ReceiveEndpoint("search-auction-created", e =>
		{
			e.UseMessageRetry(r => r.Interval(5, 5));
			e.ConfigureConsumer<AuctionCreatedConsumer>(context);
		});
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