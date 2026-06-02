using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {


        /// </summary>
        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping() => Ok("alive");


    }

}