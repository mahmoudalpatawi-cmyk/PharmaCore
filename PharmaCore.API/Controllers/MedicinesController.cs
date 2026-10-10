using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PharmaCore.Application.Catalog.DTOs;
using PharmaCore.Application.Catalog.Interfaces;
using PharmaCore.Application.Common.DTOs;
using PharmaCore.Application.Identity;

namespace PharmaCore.API.Controllers;

/// <summary>
/// Controller for managing the Medicine Catalog within the tenant context.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MedicinesController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly IValidator<CreateMedicineDto> _createValidator;
    private readonly IValidator<UpdateMedicineDto> _updateValidator;

    public MedicinesController(
        ICatalogService catalogService,
        IValidator<CreateMedicineDto> createValidator,
        IValidator<UpdateMedicineDto> updateValidator)
    {
        _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
    }

    /// <summary>
    /// Retrieves a paginated list of medicines for the authenticated tenant with optional search.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    /// <param name="search">Optional search term matching Name, TradeName, GenericName, or Barcode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated medicine catalog result.</returns>
    [Authorize(Policy = AppPolicies.RequirePharmacyStaff)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<MedicineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _catalogService.GetMedicinesPagedAsync(page, pageSize, search, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a specific medicine by its unique identifier for the authenticated tenant.
    /// </summary>
    /// <param name="id">Medicine integer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Medicine details or 404 Not Found.</returns>
    [Authorize(Policy = AppPolicies.RequirePharmacyStaff)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MedicineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "Medicine ID must be greater than zero."
            });
        }

        var medicine = await _catalogService.GetMedicineByIdAsync(id, cancellationToken);
        return Ok(medicine);
    }

    /// <summary>
    /// Creates a new medicine entry in the catalog for the authenticated tenant.
    /// </summary>
    /// <param name="dto">Medicine creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created medicine with 201 Created and Location header.</returns>
    [Authorize(Policy = AppPolicies.RequireOwnerOrAdmin)]
    [HttpPost]
    [ProducesResponseType(typeof(MedicineDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateMedicineDto dto, CancellationToken cancellationToken = default)
    {
        if (dto == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "Medicine creation payload is required."
            });
        }

        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return ValidationProblem(new ValidationProblemDetails(errors)
            {
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred."
            });
        }

        var createdMedicine = await _catalogService.CreateMedicineAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = createdMedicine.Id }, createdMedicine);
    }

    /// <summary>
    /// Updates an existing medicine entry in the catalog for the authenticated tenant.
    /// </summary>
    /// <param name="id">Medicine integer ID.</param>
    /// <param name="dto">Medicine update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated medicine details or appropriate error status.</returns>
    [Authorize(Policy = AppPolicies.RequireOwnerOrAdmin)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MedicineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateMedicineDto dto, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "Medicine ID must be greater than zero."
            });
        }

        if (dto == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "Medicine update payload is required."
            });
        }

        if (dto.Id != id)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = $"Route ID '{id}' does not match payload ID '{dto.Id}'."
            });
        }

        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return ValidationProblem(new ValidationProblemDetails(errors)
            {
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred."
            });
        }

        var updatedMedicine = await _catalogService.UpdateMedicineAsync(dto, cancellationToken);
        return Ok(updatedMedicine);
    }
}
