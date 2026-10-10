using System.Threading;
using System.Threading.Tasks;
using PharmaCore.Application.Identity.DTOs;

namespace PharmaCore.Application.Identity.Interfaces;

/// <summary>
/// Service abstraction responsible for multi-tenant user authentication and login.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user within a resolved tenant, verifies credentials, and issues an access token.
    /// Throws UnauthorizedAccessException with a generic error message upon any authentication failure.
    /// </summary>
    /// <param name="request">The tenant code, user email, and password payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login response containing the issued JWT token and user profile details.</returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
