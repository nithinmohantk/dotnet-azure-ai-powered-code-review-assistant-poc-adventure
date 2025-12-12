// src/WebApi/CodeReviewAssistant.WebApi/Features/HealthChecks/HealthCheckController.cs
using Microsoft.AspNetCore.Mvc;

namespace CodeReviewAssistant.WebApi.Features.HealthChecks
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
        }
    }
}