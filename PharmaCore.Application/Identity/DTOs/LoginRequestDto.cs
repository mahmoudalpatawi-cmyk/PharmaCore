namespace PharmaCore.Application.Identity.DTOs;

/// <summary>
/// Data transfer object for user login requests.
/// Multi-tenant login requires TenantCode, Email, and Password.
/// </summary>
public class LoginRequestDto
{
    public string TenantCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
