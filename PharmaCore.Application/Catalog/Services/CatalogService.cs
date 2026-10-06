using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using PharmaCore.Application.Catalog.DTOs;
using PharmaCore.Application.Catalog.Interfaces;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;

namespace PharmaCore.Application.Catalog.Services;

public class CatalogService : ICatalogService
{
    private readonly IRepository<Medicine> _medicineRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;

    public CatalogService(
        IRepository<Medicine> medicineRepository,
        IRepository<Category> categoryRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IMapper mapper)
    {
        _medicineRepository = medicineRepository ?? throw new ArgumentNullException(nameof(medicineRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        
        var category = _mapper.Map<Category>(dto);
        category.TenantId = tenantId;

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var categories = await _categoryRepository.ListAsync(c => c.TenantId == tenantId, cancellationToken);
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<MedicineDto> CreateMedicineAsync(CreateMedicineDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category == null || category.TenantId != tenantId)
            throw new UnauthorizedAccessException("Category not found or does not belong to the current tenant.");

        var medicine = _mapper.Map<Medicine>(dto);
        medicine.TenantId = tenantId;

        await _medicineRepository.AddAsync(medicine, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<MedicineDto>(medicine);
    }

    public async Task<MedicineDto> UpdateMedicineAsync(UpdateMedicineDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var medicine = await _medicineRepository.GetByIdAsync(dto.Id, cancellationToken);
        if (medicine == null || medicine.TenantId != tenantId)
            throw new UnauthorizedAccessException("Medicine not found or does not belong to the current tenant.");

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category == null || category.TenantId != tenantId)
            throw new UnauthorizedAccessException("Category not found or does not belong to the current tenant.");

        _mapper.Map(dto, medicine);

        await _medicineRepository.UpdateAsync(medicine, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<MedicineDto>(medicine);
    }

    public async Task<MedicineDto> GetMedicineByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var medicine = await _medicineRepository.GetByIdAsync(id, cancellationToken);

        if (medicine == null || medicine.TenantId != tenantId)
            throw new UnauthorizedAccessException("Medicine not found or does not belong to the current tenant.");

        return _mapper.Map<MedicineDto>(medicine);
    }

    public async Task<IEnumerable<MedicineDto>> GetMedicinesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var medicines = await _medicineRepository.ListAsync(m => m.TenantId == tenantId, cancellationToken);
        return _mapper.Map<IEnumerable<MedicineDto>>(medicines);
    }
}
