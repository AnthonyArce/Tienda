using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiVersion(1)]
    public class BaseApiController:ControllerBase
    {
    }
}
