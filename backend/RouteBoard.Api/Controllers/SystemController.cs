
using Microsoft.AspNetCore.Mvc;

namespace RouteBoard.Api.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    [HttpGet]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            service = "RouteBoard API",
            status = "running"
        });
    }
}
