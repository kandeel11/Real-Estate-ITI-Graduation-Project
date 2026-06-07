using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ICurrentUserService _currentUserService;
        public TestController(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        /// </summary>
        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping() => Ok("alive");

        [HttpGet("whoami")]
        [Authorize]
        public IActionResult WhoAmI()
        {
            var userId = _currentUserService.UserId;
            var email = _currentUserService.Email;
            var roles = _currentUserService.Role;

            return Ok(new
            {
                UserId = userId,
                Email = email,
                Roles = roles
            });
        }

    }

}