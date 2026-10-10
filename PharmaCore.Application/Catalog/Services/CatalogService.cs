using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using PharmaCore.Application.Catalog.DTOs;
using PharmaCore.Application.Catalog.Interfaces;
using PharmaCore.Application.Common.DTOs;
using PharmaCore.Application.Common.Exceptions;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;

namespace PharmaCore.Application.Catalog.Services;

public class CatalogService : ICatalogService
{
    private readonly IRepository<Medicine> _medicineRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<Manufacturer> _manufacturerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;

    public CatalogService(
        IRepository<Medicine> medicineRepository,
        IRepository<Category> categoryRepository,
        IRepository<Manufacturer> manufacturerRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IMapper mapper)
    {
        _medicineRepository = medicineRepository ?? throw new ArgumentNullException(nameof(medicineRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _manufacturerRepository = manufacturerRepository ?? throw new ArgumentNullException(nameof(manufacturerRepository));
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
        if (category == null || category.TenantId != tenantId || category.IsDeleted)
            throw new UnauthorizedAccessException("Category not found or does not belong to the current tenant.");

        var manufacturer = await _manufacturerRepository.GetByIdAsync(dto.ManufacturerId, cancellationToken);
        if (manufacturer == null || manufacturer.TenantId != tenantId || manufacturer.IsDeleted)
            throw new UnauthorizedAccessException("Manufacturer not found or does not belong to the current tenant.");

        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            var existingWithBarcode = await _medicineRepository.ListAsync(
                m => m.TenantId == tenantId && m.Barcode == dto.Barcode,
                cancellationToken);
            if (existingWithBarcode.Any())
            {
                throw new ConflictException($"A medicine with barcode '{dto.Barcode}' already exists.");
            }
        }

        var medicine = _mapper.Map<Medicine>(dto);
        medicine.TenantId = tenantId;

        await _medicineRepository.AddAsync(medicine, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsBarcodeUniqueConstraintViolation(ex))
        {
            throw new ConflictException($"A medicine with barcode '{dto.Barcode}' already exists.");
        }

        medicine.Category = category;
        medicine.Manufacturer = manufacturer;

        return _mapper.Map<MedicineDto>(medicine);
    }

    public async Task<MedicineDto> UpdateMedicineAsync(UpdateMedicineDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var medicine = await _medicineRepository.GetByIdAsync(dto.Id, cancellationToken);
        if (medicine == null || medicine.TenantId != tenantId || medicine.IsDeleted)
            throw new NotFoundException($"Medicine with ID {dto.Id} was not found.");

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category == null || category.TenantId != tenantId || category.IsDeleted)
            throw new UnauthorizedAccessException("Category not found or does not belong to the current tenant.");

        var manufacturer = await _manufacturerRepository.GetByIdAsync(dto.ManufacturerId, cancellationToken);
        if (manufacturer == null || manufacturer.TenantId != tenantId || manufacturer.IsDeleted)
            throw new UnauthorizedAccessException("Manufacturer not found or does not belong to the current tenant.");

        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            var existingWithBarcode = await _medicineRepository.ListAsync(
                m => m.TenantId == tenantId && m.Barcode == dto.Barcode && m.Id != dto.Id,
                cancellationToken);
            if (existingWithBarcode.Any())
            {
                throw new ConflictException($"A medicine with barcode '{dto.Barcode}' already exists.");
            }
        }

        _mapper.Map(dto, medicine);

        await _medicineRepository.UpdateAsync(medicine, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsBarcodeUniqueConstraintViolation(ex))
        {
            throw new ConflictException($"A medicine with barcode '{dto.Barcode}' already exists.");
        }

        medicine.Category = category;
        medicine.Manufacturer = manufacturer;

        return _mapper.Map<MedicineDto>(medicine);
    }

    public async Task<MedicineDto> GetMedicineByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var medicine = await _medicineRepository.GetByIdAsync(id, cancellationToken);

        if (medicine == null || medicine.TenantId != tenantId || medicine.IsDeleted)
            throw new NotFoundException($"Medicine with ID {id} was not found.");

        if (medicine.Category == null && medicine.CategoryId > 0)
        {
            medicine.Category = await _categoryRepository.GetByIdAsync(medicine.CategoryId, cancellationToken);
        }

        if (medicine.Manufacturer == null && medicine.ManufacturerId > 0)
        {
            medicine.Manufacturer = await _manufacturerRepository.GetByIdAsync(medicine.ManufacturerId, cancellationToken);
        }

        return _mapper.Map<MedicineDto>(medicine);
    }

    public async Task<IEnumerable<MedicineDto>> GetMedicinesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var medicines = (await _medicineRepository.ListAsync(m => m.TenantId == tenantId, cancellationToken)).ToList();

        if (medicines.Count > 0)
        {
            var categoryIds = medicines.Select(m => m.CategoryId).Where(id => id > 0).Distinct().ToList();
            if (categoryIds.Count > 0)
            {
                var categories = (await _categoryRepository.ListAsync(c => categoryIds.Contains(c.Id) && c.TenantId == tenantId, cancellationToken))
                    .ToDictionary(c => c.Id);

                foreach (var medicine in medicines)
                {
                    if (categories.TryGetValue(medicine.CategoryId, out var category))
                    {
                        medicine.Category = category;
                    }
                }
            }

            var manufacturerIds = medicines.Select(m => m.ManufacturerId).Where(id => id > 0).Distinct().ToList();
            if (manufacturerIds.Count > 0)
            {
                var manufacturers = (await _manufacturerRepository.ListAsync(m => manufacturerIds.Contains(m.Id) && m.TenantId == tenantId, cancellationToken))
                    .ToDictionary(m => m.Id);

                foreach (var medicine in medicines)
                {
                    if (manufacturers.TryGetValue(medicine.ManufacturerId, out var manufacturer))
                    {
                        medicine.Manufacturer = manufacturer;
                    }
                }
            }
        }

        return _mapper.Map<IEnumerable<MedicineDto>>(medicines);
    }

    public async Task<PagedResultDto<MedicineDto>> GetMedicinesPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        Expression<Func<Medicine, bool>> predicate;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            predicate = m => m.TenantId == tenantId &&
                (m.Name.Contains(term) ||
                 m.TradeName.Contains(term) ||
                 m.GenericName.Contains(term) ||
                 (m.Barcode != null && m.Barcode.Contains(term)));
        }
        else
        {
            predicate = m => m.TenantId == tenantId;
        }

        var (items, totalCount) = await _medicineRepository.GetPagedAsync(
            predicate,
            safePage,
            safePageSize,
            cancellationToken);

        var medicines = items.ToList();

        if (medicines.Count > 0)
        {
            var categoryIds = medicines.Select(m => m.CategoryId).Where(id => id > 0).Distinct().ToList();
            if (categoryIds.Count > 0)
            {
                var categories = (await _categoryRepository.ListAsync(c => categoryIds.Contains(c.Id) && c.TenantId == tenantId, cancellationToken))
                    .ToDictionary(c => c.Id);

                foreach (var medicine in medicines)
                {
                    if (categories.TryGetValue(medicine.CategoryId, out var category))
                    {
                        medicine.Category = category;
                    }
                }
            }

            var manufacturerIds = medicines.Select(m => m.ManufacturerId).Where(id => id > 0).Distinct().ToList();
            if (manufacturerIds.Count > 0)
            {
                var manufacturers = (await _manufacturerRepository.ListAsync(m => manufacturerIds.Contains(m.Id) && m.TenantId == tenantId, cancellationToken))
                    .ToDictionary(m => m.Id);

                foreach (var medicine in medicines)
                {
                    if (manufacturers.TryGetValue(medicine.ManufacturerId, out var manufacturer))
                    {
                        medicine.Manufacturer = manufacturer;
                    }
                }
            }
        }

        var dtos = _mapper.Map<List<MedicineDto>>(medicines);
        return new PagedResultDto<MedicineDto>(dtos, totalCount, safePage, safePageSize);
    }

    private static bool IsBarcodeUniqueConstraintViolation(Exception ex)
    {
        var current = ex;
        while (current != null)
        {
            if (current.Message.Contains("IX_Medicines_TenantId_Barcode", StringComparison.OrdinalIgnoreCase)
                || (current.Message.Contains("Barcode", StringComparison.OrdinalIgnoreCase) &&
                    (current.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) || current.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))))
            {
                return true;
            }
            current = current.InnerException;
        }
        return false;
    }
}
