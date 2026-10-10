using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmaCore.Application.Common.Exceptions;

namespace PharmaCore.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        int statusCode;
        string title;
        string detail;

        if (exception is UnauthorizedAccessException)
        {
            var isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;
            statusCode = isAuthenticated ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized;
            title = isAuthenticated ? "Forbidden" : "Unauthorized";
            detail = exception.Message;
        }
        else if (exception is NotFoundException or KeyNotFoundException)
        {
            statusCode = StatusCodes.Status404NotFound;
            title = "Not Found";
            detail = exception.Message;
        }
        else if (exception is ConflictException)
        {
            statusCode = StatusCodes.Status409Conflict;
            title = "Conflict";
            detail = exception.Message;
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            statusCode = StatusCodes.Status409Conflict;
            title = "Conflict";
            detail = "The record was modified concurrently by another process. Please reload and retry.";
        }
        else if (exception is DbUpdateException dbEx && IsBarcodeConflict(dbEx))
        {
            statusCode = StatusCodes.Status409Conflict;
            title = "Conflict";
            detail = "A medicine with the specified barcode already exists.";
        }
        else if (exception is InvalidOperationException or ArgumentException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            title = "Bad Request";
            detail = exception.Message;
        }
        else
        {
            statusCode = StatusCodes.Status500InternalServerError;
            title = "Server Error";
            detail = "An unexpected error occurred. Please try again later.";
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static bool IsBarcodeConflict(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("IX_Medicines_TenantId_Barcode", StringComparison.OrdinalIgnoreCase)
            || (msg.Contains("Barcode", StringComparison.OrdinalIgnoreCase) &&
                (msg.Contains("unique", StringComparison.OrdinalIgnoreCase) || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase)));
    }
}
