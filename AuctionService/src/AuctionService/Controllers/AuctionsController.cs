using System;
using AuctionService.Data;
using AuctionService.DTOs;
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
}
