using AutoMapper;
using PharmaCore.Application.Catalog.DTOs;
using PharmaCore.Domain.Entities.Catalog;

namespace PharmaCore.Application.Catalog.Mappings;

public class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        CreateMap<Category, CategoryDto>();
        CreateMap<CreateCategoryDto, Category>();

        CreateMap<Medicine, MedicineDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));
            
        CreateMap<CreateMedicineDto, Medicine>();
        
        CreateMap<UpdateMedicineDto, Medicine>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()); // Prevent overwriting ID
    }
}
