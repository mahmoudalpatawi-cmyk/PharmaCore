using AutoMapper;
using PharmaCore.Application.Inventory.DTOs;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Application.Inventory.Mappings;

public class InventoryMappingProfile : Profile
{
    public InventoryMappingProfile()
    {
        CreateMap<Batch, BatchResponseDto>();
        
        CreateMap<StockMovement, StockMovementResponseDto>()
            .ForMember(dest => dest.MovementType, opt => opt.MapFrom(src => src.MovementType.ToString()));
    }
}
