using Microsoft.EntityFrameworkCore;
using AuctionService.Entities;

namespace AuctionService.Data;

public class AuctionDbContext : DbContext
{
	public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options)
	{
	}

	public DbSet<Auction> Auctions { get; set; }

}
