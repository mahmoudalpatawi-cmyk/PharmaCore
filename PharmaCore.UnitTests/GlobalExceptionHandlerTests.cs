using System;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PharmaCore.API.Middleware;
using PharmaCore.Application.Common.Exceptions;
using Xunit;

namespace PharmaCore.UnitTests;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler = new(NullLogger<GlobalExceptionHandler>.Instance);

    private async Task<(int statusCode, ProblemDetails details)> ExecuteHandlerAsync(Exception exception, bool isAuthenticated = false)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        if (isAuthenticated)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth");
            context.User = new ClaimsPrincipal(identity);
        }

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);
        Assert.True(handled);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var details = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(details);
        return (context.Response.StatusCode, details!);
    }

    [Fact]
    public async Task NotFoundException_MapsTo404NotFound()
    {
        var (statusCode, details) = await ExecuteHandlerAsync(new NotFoundException("Medicine 123 was not found."));

        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
        Assert.Equal(404, details.Status);
        Assert.Equal("Not Found", details.Title);
        Assert.Equal("Medicine 123 was not found.", details.Detail);
    }

    [Fact]
    public async Task ConflictException_MapsTo409Conflict()
    {
        var (statusCode, details) = await ExecuteHandlerAsync(new ConflictException("A medicine with barcode '12345' already exists."));

        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        Assert.Equal(409, details.Status);
        Assert.Equal("Conflict", details.Title);
        Assert.Equal("A medicine with barcode '12345' already exists.", details.Detail);
    }

    [Fact]
    public async Task DbUpdateConcurrencyException_MapsTo409Conflict()
    {
        var concurrencyEx = new DbUpdateConcurrencyException("Concurrency conflict occurred.");

        var (statusCode, details) = await ExecuteHandlerAsync(concurrencyEx);

        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        Assert.Equal(409, details.Status);
        Assert.Equal("Conflict", details.Title);
        Assert.Equal("The record was modified concurrently by another process. Please reload and retry.", details.Detail);
    }

    [Fact]
    public async Task DbUpdateException_WithBarcodeConstraint_MapsTo409Conflict()
    {
        var innerEx = new Exception("Violation of UNIQUE KEY constraint 'IX_Medicines_TenantId_Barcode'. Cannot insert duplicate key in object 'dbo.Medicines'.");
        var dbEx = new DbUpdateException("An error occurred while saving the entity changes.", innerEx);

        var (statusCode, details) = await ExecuteHandlerAsync(dbEx);

        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        Assert.Equal(409, details.Status);
        Assert.Equal("Conflict", details.Title);
        Assert.Equal("A medicine with the specified barcode already exists.", details.Detail);
    }

    [Fact]
    public async Task DbUpdateException_GenericWithoutBarcode_MapsTo500InternalServerError()
    {
        var innerEx = new Exception("Violation of FOREIGN KEY constraint 'FK_SomeTable'.");
        var dbEx = new DbUpdateException("An error occurred while saving the entity changes.", innerEx);

        var (statusCode, details) = await ExecuteHandlerAsync(dbEx);

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.Equal(500, details.Status);
        Assert.Equal("Server Error", details.Title);
        Assert.Equal("An unexpected error occurred. Please try again later.", details.Detail);
    }

    [Fact]
    public async Task UnauthorizedAccessException_AuthenticatedUser_MapsTo403Forbidden()
    {
        var (statusCode, details) = await ExecuteHandlerAsync(new UnauthorizedAccessException("User is not authorized."), isAuthenticated: true);

        Assert.Equal(StatusCodes.Status403Forbidden, statusCode);
        Assert.Equal(403, details.Status);
        Assert.Equal("Forbidden", details.Title);
        Assert.Equal("User is not authorized.", details.Detail);
    }

    [Fact]
    public async Task UnauthorizedAccessException_UnauthenticatedUser_MapsTo401Unauthorized()
    {
        var (statusCode, details) = await ExecuteHandlerAsync(new UnauthorizedAccessException("Authentication required."), isAuthenticated: false);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
        Assert.Equal(401, details.Status);
        Assert.Equal("Unauthorized", details.Title);
        Assert.Equal("Authentication required.", details.Detail);
    }

    [Fact]
    public async Task InvalidOperationException_MapsTo400BadRequest()
    {
        var (statusCode, details) = await ExecuteHandlerAsync(new InvalidOperationException("Invalid state transition."));

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        Assert.Equal(400, details.Status);
        Assert.Equal("Bad Request", details.Title);
        Assert.Equal("Invalid state transition.", details.Detail);
    }
}
