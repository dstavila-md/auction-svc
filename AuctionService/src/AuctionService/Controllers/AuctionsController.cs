using System;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
	private readonly AuctionDbContext _context;
	private readonly IMapper _mapper;
	public AuctionsController(AuctionDbContext context, IMapper mapper)
	{
		this._context = context;
		this._mapper = mapper;
	}

	[HttpGet]
	public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions()
	{
		var auctions = await this._context.Auctions.Include(x => x.Item).OrderBy(x => x.Item.Make).ToListAsync();
		return this._mapper.Map<List<AuctionDto>>(auctions);
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
		var result = await _context.SaveChangesAsync() > 0;
		if (!result) return BadRequest("Failed to create auction");
		return CreatedAtAction(nameof(GetAuctionById), new { id = auction.Id }, this._mapper.Map<AuctionDto>(auction));
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

		var result = await this._context.SaveChangesAsync() > 0;

		if (result) return Ok();

		return BadRequest("Problem saving changes");
	}

}
