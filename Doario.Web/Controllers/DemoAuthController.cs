using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doario.Web.Controllers;

/// <summary>
/// Simple demo authentication — single credential stored in config.
/// Uses a cookie to maintain the session.
/// </summary>
[ApiController]
[Route("api/demo-auth")]
[AllowAnonymous]
public class DemoAuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private const string CookieName = "doario_demo";

    public DemoAuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] DemoLoginRequest request)
    {
        var validUsername = _config["DemoAuth:Username"];
        var validPassword = _config["DemoAuth:Password"];

        if (string.IsNullOrWhiteSpace(validUsername) || string.IsNullOrWhiteSpace(validPassword))
            return StatusCode(500, new { error = "Demo auth not configured." });

        if (!string.Equals(request.Username, validUsername, StringComparison.OrdinalIgnoreCase)
            || request.Password != validPassword)
            return Unauthorized(new { error = "Invalid credentials." });

        // Set a secure cookie valid for 24 hours
        Response.Cookies.Append(CookieName, "authenticated", new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // set to true in production with HTTPS
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(24),
        });

        return Ok(new { success = true });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieName);
        return Ok(new { success = true });
    }

    [HttpGet("check")]
    public IActionResult Check()
    {
        var cookie = Request.Cookies[CookieName];
        return Ok(new { authenticated = cookie == "authenticated" });
    }
}

public class DemoLoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}