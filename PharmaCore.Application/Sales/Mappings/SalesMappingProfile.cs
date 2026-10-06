using AutoMapper;
using PharmaCore.Application.Sales.DTOs;
using PharmaCore.Domain.Entities.Sales;

namespace PharmaCore.Application.Sales.Mappings;

public class SalesMappingProfile : Profile
{
    public SalesMappingProfile()
    {
        CreateMap<SaleInvoice, SaleInvoiceResponseDto>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : null))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
