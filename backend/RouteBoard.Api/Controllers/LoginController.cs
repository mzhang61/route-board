using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RouteBoard.Api.Controllers;

[ApiController]
public sealed class LoginController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("/api/auth/authorize")]
    [EnableRateLimiting("login")]
    public IActionResult Authorize()
    {
        if (!Uri.TryCreate(configuration["Authentication:Authority"], UriKind.Absolute, out var issuer)
            || issuer.Scheme != Uri.UriSchemeHttps)
            return Problem(statusCode: 503, title: "Sign-in is unavailable.");

        // Auth0 validates the SPA client and callback. Keep its destination fixed by server configuration.
        var endpoint = new Uri(issuer, "/authorize");
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return Redirect(endpoint.AbsoluteUri + Request.QueryString.ToUriComponent());
    }
}
