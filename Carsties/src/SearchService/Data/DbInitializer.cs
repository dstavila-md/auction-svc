using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Data;

public class DbInitializer
{
	public static async Task InitDb(WebApplication app)
	{
		// Initialize MongoDB connection and set up the database
		await DB.InitAsync("SearchDb", MongoClientSettings.FromConnectionString(app.Configuration.GetConnectionString("MongoDbConnection")));

		// Create text index on Make, Model, and Color fields for efficient search
		await DB.Index<Item>()
			.Key(x => x.Make, KeyType.Text)
			.Key(x => x.Model, KeyType.Text)
			.Key(x => x.Color, KeyType.Text)
			.CreateAsync();

		var count = await DB.CountAsync<Item>();
		if (count == 0)
		{
			Console.WriteLine("No data - will attempt to seed");

			// Read initial data from JSON file and insert into MongoDB
			var itemData = await File.ReadAllTextAsync("Data/auction.json");

			// Set up JSON serializer options to ignore case when matching property names
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			// Deserialize the JSON data into a list of Item objects
			var items = JsonSerializer.Deserialize<List<Item>>(itemData, options);

			// Save the deserialized items to the MongoDB database
			await DB.SaveAsync(items);
		}
	}
}
