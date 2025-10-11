using Microsoft.AspNetCore.Mvc;

namespace BlogApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelloController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Pong from API!");
        }
    }
}
