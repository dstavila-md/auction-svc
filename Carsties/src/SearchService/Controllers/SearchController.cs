using System;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
	[HttpGet]
	public async Task<ActionResult<List<Item>>> SearchItems(string searchTerm, int pageNumber = 1, int pageSize = 5)
	{

		var query = DB.PagedSearch<Item>();

		query.Sort(x => x.Ascending(x => x.Make));

		if (!string.IsNullOrEmpty(searchTerm))
		{
			// query.Match(x => x.Make.Contains(searchTerm) || x.Model.Contains(searchTerm));

			// performs a full-text search on the MongoDB collection 
			// using the Match method with Search.Full, which searches 
			// across all text-indexed fields in the documents. It then sorts 
			// the results by relevance score using SortByTextScore(), ensuring 
			// the most relevant matches appear first. This requires a text index 
			// to be created on the relevant fields.
			query.Match(Search.Full, searchTerm).SortByTextScore();
		}

		query.PageNumber(pageNumber);
		query.PageSize(pageSize);

		var results = await query.ExecuteAsync();

		return Ok(new
		{
			results = results.Results,
			pageCount = results.PageCount,
			totalCount = results.TotalCount
		});
	}
}
