using System;
using MassTransit;
using Contracts;
using AutoMapper;
using SearchService.Models;
using MongoDB.Entities;

namespace SearchService.Consumers;

public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
	private readonly IMapper _mapper;

	public AuctionUpdatedConsumer(IMapper mapper)
	{
		this._mapper = mapper;
	}

	public async Task Consume(ConsumeContext<AuctionUpdated> context)
	{
		var message = context.Message;
		Console.WriteLine($"Received AuctionUpdated event: Id={message.Id}, Make={message.Make}, Model={message.Model}, Year={message.Year}");

		// Update item in database or search index
		await DB.Update<Item>()
						.MatchID(message.Id)
						.Modify(item => item.Make, message.Make)
						.Modify(item => item.Model, message.Model)
						.Modify(item => item.Year, message.Year)
						.Modify(item => item.Color, message.Color)
						.Modify(item => item.Mileage, message.Mileage)
						.ExecuteAsync();

	}

}
