using Microsoft.EntityFrameworkCore;
using AuctionService.Data;
using AuctionService.RequestHelpers;
using MassTransit;
using AuctionService.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(options =>
		options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddAutoMapper(typeof(MappingProfiles).Assembly);

builder.Services.AddMassTransit(x =>
{
	x.AddEntityFrameworkOutbox<AuctionDbContext>(o =>
	{
		o.QueryDelay = TimeSpan.FromSeconds(10);
		o.UsePostgres();
		o.UseBusOutbox();
	});

	x.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>(); // Add this line to register the consumer
	x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false)); // Use kebab case for endpoint names

	// Register consumers here
	x.UsingRabbitMq((context, cfg) =>
	{
		// Configure RabbitMQ settings here
		cfg.ConfigureEndpoints(context);
	});
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

try
{
	DbInitializer.InitDb(app);
}
catch (Exception ex)
{
	Console.WriteLine($"Error initializing database: {ex.Message}");
}

app.Run();
