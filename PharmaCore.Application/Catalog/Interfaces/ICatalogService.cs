using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PharmaCore.Application.Catalog.DTOs;

namespace PharmaCore.Application.Catalog.Interfaces;

public interface ICatalogService
{
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    
    Task<MedicineDto> CreateMedicineAsync(CreateMedicineDto dto, CancellationToken cancellationToken = default);
    Task<MedicineDto> UpdateMedicineAsync(UpdateMedicineDto dto, CancellationToken cancellationToken = default);
    Task<MedicineDto> GetMedicineByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<MedicineDto>> GetMedicinesAsync(CancellationToken cancellationToken = default);
}
