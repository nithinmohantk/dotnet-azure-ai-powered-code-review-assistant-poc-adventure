// src/SharedKernel/CodeReviewAssistant.SharedKernel/Web/ApiControllerBase.cs
using Microsoft.AspNetCore.Mvc;

namespace CodeReviewAssistant.SharedKernel.Web
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult HandleResult<T>(T result) where T : class
        {
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}