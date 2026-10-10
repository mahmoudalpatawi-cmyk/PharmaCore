using System.Threading.Tasks;
using PharmaCore.Domain.Entities.Identity;

namespace PharmaCore.Application.Identity.Interfaces;

/// <summary>
/// Service abstraction responsible strictly for generating cryptographically signed
/// JSON Web Tokens (JWT) containing approved claims for validated application users.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT token containing the approved security claims for the specified user.
    /// </summary>
    /// <param name="user">The already authenticated and validated ApplicationUser with Role navigation loaded.</param>
    /// <returns>A signed JWT token string.</returns>
    Task<string> GenerateTokenAsync(ApplicationUser user);
}
