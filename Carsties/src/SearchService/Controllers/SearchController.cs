using System;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.RequestHelpers;

namespace SearchService.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
	[HttpGet]
	public async Task<ActionResult<List<Item>>> SearchItems([FromQuery] SearchParams searchParams) // [FromQuery] allows the API to bind query string parameters to the SearchParams object, enabling clients to specify search criteria directly in the URL when making a GET request.
	{

		var query = DB.PagedSearch<Item, Item>();

		if (!string.IsNullOrEmpty(searchParams.SearchTerm))
		{
			// Example of a simple search using the Match method to filter items based on the Make or Model properties containing the search term. 
			// This is a basic implementation and may not be as efficient or relevant as a full-text search, especially for larger datasets.
			// query.Match(x => x.Make.Contains(searchParams.SearchTerm) || x.Model.Contains(searchParams.SearchTerm));

			// performs a full-text search on the MongoDB collection 
			// using the Match method with Search.Full, which searches 
			// across all text-indexed fields in the documents. It then sorts 
			// the results by relevance score using SortByTextScore(), ensuring 
			// the most relevant matches appear first. This requires a text index 
			// to be created on the relevant fields.
			query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
		}

		// The switch expression evaluates the OrderBy property of the searchParams object 
		// and applies the corresponding sorting logic to the query.
		query = searchParams.OrderBy switch
		{
			"make" => query.Sort(x => x.Ascending(a => a.Make)), // Sorts the results in ascending order based on the Make property of the Item documents.
			"new" => query.Sort(x => x.Descending(a => a.CreatedAt)), // Sorts the results in descending order based on the CreatedAt property, showing the newest items first.
			_ => query.Sort(x => x.Ascending(a => a.AuctionEnd)) // Default sorting by ascending order of the AuctionEnd property, showing items that are ending soonest first.
		};

		query = searchParams.FilterBy switch
		{
			"finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow), // Filters the results to include only items whose AuctionEnd date is in the past, indicating that the auction has finished.
			"endingSoon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6) && x.AuctionEnd > DateTime.UtcNow), // Filters the results to include only items whose AuctionEnd date is within the next 6 hours, indicating that the auction is ending soon.
			_ => query.Match(x => x.AuctionEnd > DateTime.UtcNow) // Default filter to include only items whose AuctionEnd date is in the future, indicating that the auction is still active.
		};

		if (!string.IsNullOrEmpty(searchParams.Seller))
		{
			query.Match(x => x.Seller == searchParams.Seller); // Filters the results to include only items that match the specified SellerId, allowing users to search for items from a specific seller.
		}

		if (!string.IsNullOrEmpty(searchParams.Winner))
		{
			query.Match(x => x.Winner == searchParams.Winner); // Filters the results to include only items that match the specified Winner, allowing users to search for items they have won.
		}

		query.PageNumber(searchParams.PageNumber);
		query.PageSize(searchParams.PageSize);

		var results = await query.ExecuteAsync();

		return Ok(new
		{
			results = results.Results,
			pageCount = results.PageCount,
			totalCount = results.TotalCount
		});
	}
}
