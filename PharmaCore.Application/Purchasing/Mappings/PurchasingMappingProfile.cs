using AutoMapper;
using PharmaCore.Application.Purchasing.DTOs;
using PharmaCore.Domain.Entities.Purchasing;

namespace PharmaCore.Application.Purchasing.Mappings;

public class PurchasingMappingProfile : Profile
{
    public PurchasingMappingProfile()
    {
        CreateMap<Supplier, SupplierDto>();
        
        CreateMap<PurchaseInvoice, PurchaseInvoiceResponseDto>()
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
