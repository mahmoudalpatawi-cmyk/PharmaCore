using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PharmaCore.API;
using PharmaCore.Infrastructure.Data;
using PharmaCore.UnitTests.Controllers;

namespace PharmaCore.UnitTests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureTestServices;
    private readonly Dictionary<string, string?>? _configurationOverrides;
    private readonly string _databaseName;

    public CustomWebApplicationFactory(
        Action<IServiceCollection>? configureTestServices = null,
        Dictionary<string, string?>? configurationOverrides = null)
    {
        _configureTestServices = configureTestServices;
        _configurationOverrides = configurationOverrides;
        _databaseName = "IntegrationTestDb_" + Guid.NewGuid();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        if (_configurationOverrides != null && _configurationOverrides.Any())
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(_configurationOverrides);
            });
        }

        builder.ConfigureServices(services =>
        {
            // Remove real SQL Server DbContext registrations and options
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ApplicationDbContext) ||
                (d.ServiceType.FullName != null && (
                    d.ServiceType.FullName.Contains("DbContextOptions") ||
                    d.ServiceType.FullName.Contains("ApplicationDbContext")
                ))
            ).ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Replace with isolated In-Memory database per factory instance
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Register TestAuthController assembly so isolated test endpoints can be routed
            services.AddControllers()
                .AddApplicationPart(typeof(TestAuthController).Assembly);

            _configureTestServices?.Invoke(services);
        });
    }
}
