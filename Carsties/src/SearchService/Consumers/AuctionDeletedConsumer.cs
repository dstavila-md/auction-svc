using System;
using MassTransit;
using Contracts;
using AutoMapper;
using SearchService.Models;
using MongoDB.Entities;

namespace SearchService.Consumers;

public class AuctionDeletedConsumer : IConsumer<AuctionDeleted>
{

	public async Task Consume(ConsumeContext<AuctionDeleted> context)
	{
		var message = context.Message;
		Console.WriteLine($"Received AuctionDeleted event: Id={message.Id}");

		var result = await DB.DeleteAsync<Item>(message.Id);

		if (!result.IsAcknowledged)
		{
			throw new MessageException(typeof(AuctionUpdated), "Problem deleting auction from MongoDB");
		}
	}

}
