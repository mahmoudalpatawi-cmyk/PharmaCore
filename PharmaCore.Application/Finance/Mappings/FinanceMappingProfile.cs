using AutoMapper;
using PharmaCore.Application.Finance.DTOs;
using PharmaCore.Domain.Entities.Finance;
using PharmaCore.Domain.Entities.Shifts;

namespace PharmaCore.Application.Finance.Mappings;

public class FinanceMappingProfile : Profile
{
    public FinanceMappingProfile()
    {
        CreateMap<Shift, ShiftResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<CashTransaction, CashTransactionResponseDto>()
            .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => src.Type.ToString()));
    }
}
