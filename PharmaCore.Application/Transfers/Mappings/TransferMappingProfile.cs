using AutoMapper;
using PharmaCore.Application.Transfers.DTOs;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Application.Transfers.Mappings;

public class TransferMappingProfile : Profile
{
    public TransferMappingProfile()
    {
        CreateMap<StockTransfer, TransferDocumentResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<StockTransferItem, TransferDocumentItemResponseDto>();
    }
}
