using System;
using MassTransit;
using Contracts;
using AutoMapper;
using SearchService.Models;
using MongoDB.Entities;

namespace SearchService.Consumers;

public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
{
	private readonly IMapper _mapper;

	public AuctionCreatedConsumer(IMapper mapper)
	{
		_mapper = mapper;
	}
	public async Task Consume(ConsumeContext<AuctionCreated> context)
	{
		var message = context.Message;
		Console.WriteLine($"Received AuctionCreated event: Id={message.Id}, Make={message.Make}, Model={message.Model}, Year={message.Year}");

		// Map the message to the Item model
		var item = _mapper.Map<Item>(message);

		// Save item to database or search index
		await item.SaveAsync(); // Assuming MongoDB or similar data
	}
}
