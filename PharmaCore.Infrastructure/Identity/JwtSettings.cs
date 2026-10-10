namespace PharmaCore.Infrastructure.Identity;

/// <summary>
/// Configuration options for JSON Web Token (JWT) authentication and issuance.
/// Bound to the "JwtSettings" configuration section in appsettings.json.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Gets or sets the valid token issuer (e.g., "PharmaCoreAPI").
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the valid token audience (e.g., "PharmaCoreClient").
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symmetric signing key used to sign and validate tokens.
    /// MUST be at least 256 bits (32 bytes) for HMAC-SHA256.
    /// In production, this must be securely provided via User Secrets or environment variables.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token lifetime in minutes. Default is 60 minutes.
    /// </summary>
    public int ExpiryMinutes { get; set; } = 60;
}
