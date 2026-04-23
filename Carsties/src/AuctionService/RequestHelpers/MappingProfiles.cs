
using AuctionService.Entities;
using AuctionService.DTOs;
using AutoMapper;
using Contracts;

namespace AuctionService.RequestHelpers;

public class MappingProfiles : Profile
{
	public MappingProfiles()
	{
		CreateMap<Auction, AuctionDto>().IncludeMembers(x => x.Item);
		CreateMap<Item, AuctionDto>();

		CreateMap<CreateAuctionDto, Auction>().ForMember(dest => dest.Item, opt => opt.MapFrom(src => src));
		CreateMap<CreateAuctionDto, Item>();
		CreateMap<AuctionDto, AuctionCreated>();

		CreateMap<Auction, AuctionUpdated>().IncludeMembers(a => a.Item);
		CreateMap<Item, AuctionUpdated>();

		CreateMap<Auction, AuctionDeleted>().ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()));

	}
}
