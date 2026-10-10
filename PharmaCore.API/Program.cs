using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PharmaCore.Application.Identity;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Infrastructure;
using PharmaCore.Infrastructure.Data;
using PharmaCore.Infrastructure.Identity;

namespace PharmaCore.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            
            builder.Services.AddExceptionHandler<Middleware.GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // ─── JWT Authentication ────────────────────────────────────────────────
            var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
            builder.Services.Configure<JwtSettings>(jwtSection);

            var jwtSettings = jwtSection.Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");

            if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
            {
                throw new InvalidOperationException("JwtSettings:SecretKey is required.");
            }

            var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
            if (key.Length < 32)
            {
                throw new InvalidOperationException("JwtSettings:SecretKey must contain at least 32 bytes (256 bits) for HMAC-SHA256 signing.");
            }

            const string developmentPlaceholder = "DEVELOPMENT_ONLY_KEY_CHANGE_IN_PRODUCTION_MUST_BE_AT_LEAST_32_CHARS_LONG!";
            if (!builder.Environment.IsDevelopment() &&
                (jwtSettings.SecretKey == developmentPlaceholder || jwtSettings.SecretKey.Contains("DEVELOPMENT_ONLY", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "The development JWT secret key placeholder cannot be used in non-Development environments. " +
                    "Configure a secure production secret key via the 'JwtSettings__SecretKey' environment variable or a secret manager.");
            }

            if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
            {
                throw new InvalidOperationException("JwtSettings:Issuer is required.");
            }

            if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
            {
                throw new InvalidOperationException("JwtSettings:Audience is required.");
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.SaveToken = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = "role"
                };
            });

            // ─── Authorization Policies ───────────────────────────────────────────
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(AppPolicies.OwnerOnly, policy =>
                    policy.RequireRole(AppRoles.Owner));

                options.AddPolicy(AppPolicies.AdminOnly, policy =>
                    policy.RequireRole(AppRoles.Admin));

                options.AddPolicy(AppPolicies.PharmacistOnly, policy =>
                    policy.RequireRole(AppRoles.Pharmacist));

                options.AddPolicy(AppPolicies.CashierOnly, policy =>
                    policy.RequireRole(AppRoles.Cashier));

                options.AddPolicy(AppPolicies.RequireOwnerOrAdmin, policy =>
                    policy.RequireRole(AppRoles.Owner, AppRoles.Admin));

                options.AddPolicy(AppPolicies.RequirePharmacyStaff, policy =>
                    policy.RequireRole(AppRoles.Owner, AppRoles.Admin, AppRoles.Pharmacist));

                options.AddPolicy(AppPolicies.RequirePosAccess, policy =>
                    policy.RequireRole(AppRoles.Owner, AppRoles.Admin, AppRoles.Cashier));
            });

            // ─── Rate Limiting ─────────────────────────────────────────────────────
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    if (context.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                    }
                    else
                    {
                        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                        var window = config.GetValue<int>("RateLimiting:Login:WindowSeconds", 60);
                        context.HttpContext.Response.Headers.RetryAfter = window.ToString();
                    }

                    await context.HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too Many Requests",
                        Detail = "Too many login attempts. Please try again later."
                    }, cancellationToken);
                };

                options.AddPolicy(AppPolicies.LoginRateLimitPolicy, httpContext =>
                {
                    var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
                    var permitLimit = configuration.GetValue<int>("RateLimiting:Login:PermitLimit", 5);
                    var windowSeconds = configuration.GetValue<int>("RateLimiting:Login:WindowSeconds", 60);

                    // Partition requests using the connection remote IP.
                    // Trustworthy: does not blindly parse arbitrary client-supplied headers
                    // unless reverse proxy forwarding middleware is explicitly configured.
                    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client";

                    return System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: clientIp,
                        factory: _ => new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromSeconds(windowSeconds),
                            SegmentsPerWindow = 2,
                            QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });
            });

            builder.Services.AddInfrastructureServices(builder.Configuration);

            var app = builder.Build();

            app.UseExceptionHandler();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
