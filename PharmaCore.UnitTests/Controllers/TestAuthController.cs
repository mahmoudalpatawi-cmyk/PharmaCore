using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaCore.Application.Identity;

namespace PharmaCore.UnitTests.Controllers;

[ApiController]
[Route("api/test-auth")]
public class TestAuthController : ControllerBase
{
    [Authorize]
    [HttpGet("protected")]
    public IActionResult ProtectedEndpoint()
    {
        return Ok(new { message = "authenticated_access_granted" });
    }

    [Authorize(Policy = AppPolicies.OwnerOnly)]
    [HttpGet("owner-only")]
    public IActionResult OwnerOnlyEndpoint()
    {
        return Ok(new { message = "owner_access_granted" });
    }

    [Authorize(Policy = AppPolicies.AdminOnly)]
    [HttpGet("admin-only")]
    public IActionResult AdminOnlyEndpoint()
    {
        return Ok(new { message = "admin_access_granted" });
    }
}
