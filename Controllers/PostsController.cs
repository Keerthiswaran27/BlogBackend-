using Microsoft.AspNetCore.Mvc;

namespace BlogApp1.Server.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        [HttpGet("{id}/content")]
        public IActionResult GetPostContent(int id)
        {
            string htmlContent = "<p><strong>Hello from backend!</strong> This content came via API 😎</p>";
            return Ok(htmlContent);
        }
    }

}
