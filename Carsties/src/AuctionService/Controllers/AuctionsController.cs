using System;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
	private readonly AuctionDbContext _context;
	private readonly IMapper _mapper;
	private readonly IPublishEndpoint _publishEndpoint;

	public AuctionsController(AuctionDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
	{
		this._context = context;
		this._mapper = mapper;
		this._publishEndpoint = publishEndpoint;
	}

	[HttpGet]
	public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
	{// The GetAllAuctions method retrieves a list of auctions from the database, 
	 // optionally filtering them based on an updated date provided as a query parameter. 
	 // It uses Entity Framework Core to query the Auctions DbSet, 
	 // applying an optional filter to return only auctions that have been updated after the specified date. 
	 // The results are then projected to a list of AuctionDto objects using AutoMapper before being returned to the client.


		// The method starts by creating a queryable collection of auctions from the database,
		// ordered by the Make property of the associated Item.
		var query = this._context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

		if (!string.IsNullOrEmpty(date))
		{
			// If a date is provided as a query parameter, the method applies a filter to the query to return only auctions
			// that have been updated after the specified date.
			query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
		}

		// Finally, the method projects the resulting auctions to a list of AuctionDto objects using AutoMapper's ProjectTo method,
		// which allows for efficient querying and mapping in a single step, and returns the list to the client.
		return await query.ProjectTo<AuctionDto>(this._mapper.ConfigurationProvider).ToListAsync();
	}
	[HttpGet("{id}")]
	public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
	{
		var auction = await this._context.Auctions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);
		if (auction == null) return NotFound();
		return this._mapper.Map<AuctionDto>(auction);
	}

	[HttpPost]
	public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto createAuctionDto)
	{
		var auction = this._mapper.Map<Auction>(createAuctionDto);
		// TODO: add current user as seller
		auction.Seller = "test seller";
		this._context.Auctions.Add(auction);

		// publish auction created event (RabbitMQ)
		// map back to AuctionDto
		var newAuctionPub = this._mapper.Map<AuctionDto>(auction);
		// map to AuctionCreated message (from Contracts) and publish to RabbitMQ
		await this._publishEndpoint.Publish(this._mapper.Map<AuctionCreated>(newAuctionPub)); // _publishEndpoint

		// save to databse - postgres
		var result = await _context.SaveChangesAsync() > 0;

		// communicate back to the client
		if (!result) return BadRequest("Failed to create auction");
		return CreatedAtAction(nameof(GetAuctionById), new { id = auction.Id }, newAuctionPub);
	}

	[HttpPut("{id}")]
	public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
	{
		var auction = await this._context.Auctions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);
		if (auction == null) return NotFound();

		// TODO: check seller is current user

		auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
		auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
		auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
		auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
		auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

		var updatedAuctionPub = this._mapper.Map<AuctionUpdated>(auction);
		await this._publishEndpoint.Publish(updatedAuctionPub);

		var result = await this._context.SaveChangesAsync() > 0;

		if (result) return Ok();

		return BadRequest("Problem saving changes");
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteAuction(Guid id)
	{
		var auction = await this._context.Auctions.FindAsync(id);
		if (auction == null) return NotFound();

		// TODO: check seller is current user

		// Map to auctionDeleted contract
		var auctionPub = this._mapper.Map<AuctionDeleted>(auction);
		// publish the event
		await this._publishEndpoint.Publish(auctionPub);

		// The method starts by attempting to find the auction with the specified ID in the database.
		// If the auction is not found, it returns a Bad Request response to the client.
		this._context.Auctions.Remove(auction);
		// If the auction is found, it is removed from the database context, and the changes are saved to the database.
		var result = await this._context.SaveChangesAsync() > 0;
		// Finally, the method checks if the save operation was successful and returns an appropriate response to the client.
		if (!result) return BadRequest("Could not updated DB");
		return Ok();
	}
}