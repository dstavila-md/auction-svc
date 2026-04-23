using System;
using MassTransit;
using Contracts;
using SearchService.Models;
using MongoDB.Entities;
using AutoMapper;

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

		var item = this._mapper.Map<Item>(message); // Map the message

		var result = await DB.Update<Item>()
												.MatchID(message.Id)
												.ModifyOnly(x => new { x.Color, x.Make, x.Model, x.Year, x.Mileage }, item)
												.ExecuteAsync();


		// Update item in database or search index,
		// without item and automapper - alternate version
		//
		// var result = await DB.Update<Item>()
		// 				.MatchID(message.Id)
		// 				.Modify(item => item.Make, message.Make)
		// 				.Modify(item => item.Model, message.Model)
		// 				.Modify(item => item.Year, message.Year)
		// 				.Modify(item => item.Color, message.Color)
		// 				.Modify(item => item.Mileage, message.Mileage)
		// 				.ExecuteAsync();

		if (!result.IsAcknowledged)
		{
			throw new MessageException(typeof(AuctionUpdated), "Problem updating MongoDB");
		}
	}
}
