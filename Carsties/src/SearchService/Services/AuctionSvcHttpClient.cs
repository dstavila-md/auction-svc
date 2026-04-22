using System;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services;

public class AuctionSvcHttpClient
{
	private readonly IConfiguration _configuration;
	private readonly HttpClient _httpClient;
	public AuctionSvcHttpClient(HttpClient httpClient, IConfiguration configuration)
	{
		this._configuration = configuration;
		this._httpClient = httpClient;
	}

	// The GetItemsForSearchDb method retrieves a list of items from the Auction Service API
	// that have been updated since the last update time recorded in the search database.
	public async Task<List<Item>> GetItemsForSearchDb()
	{
		// The method starts by querying the search database to find the most recent update time of any item.
		// It uses the DB.Find method to search for Item documents, sorts them in descending order
		//  based on the UpdatedAt property, and projects the result to get the UpdatedAt value as a string.
		var lastUpdated = await DB.Find<Item, string>()
															.Sort(x => x.Descending(x => x.UpdatedAt))
															.Project(x => x.UpdatedAt.ToString())
															.ExecuteFirstAsync();

		// Finally, the method makes an HTTP GET request to the Auction Service API, passing the last updated time as a query parameter,
		// to retrieve a list of items that have been updated since that time. The response is expected to be a JSON array of Item objects,
		// which is deserialized into a List<Item> and returned to the caller.
		return await _httpClient.GetFromJsonAsync<List<Item>>($"{this._configuration["AuctionServiceUrl"]}/api/auctions?date={lastUpdated}");
	}

}
