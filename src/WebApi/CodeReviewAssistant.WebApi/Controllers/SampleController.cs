using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeReviewAssistant.WebApi.Controllers
{
    [ApiController]
    [Route(""api/[controller]"")]
    [Authorize]
    public class SampleController : ControllerBase
    {
        private readonly ILogger<SampleController> _logger;

        public SampleController(ILogger<SampleController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation(""Sample Get endpoint called"");
            return Ok(new { Message = ""Hello from Code Review Assistant API!"" });
        }

        [HttpGet(""secure"")]
        [Authorize(Roles = ""Admin"")]
        public IActionResult GetSecure()
        {
            return Ok(new { Message = ""This is a secure endpoint"" });
        }
    }
}
