using API.DTO;
using API.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    [ApiVersion(1)]
    public class UsuarioController : BaseApiController
    {
        private readonly IUserServices _userServices;
        public UsuarioController(IUserServices userServices)
        {
            _userServices = userServices;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterDTO registerDTO)
        {
            var result = await _userServices.RegisterAsync(registerDTO);
            if (result.Contains("exitosamente"))
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

        [HttpPost("token")]
        public async Task<IActionResult> GetTokenAsync(LoginDTO loginDTO)
        {
            var resultado = await _userServices.GetTokenAsync(loginDTO);
            SetRefreshTokenInCookie(resultado.RefreshToken);
            if (resultado == null)
            {
                return Unauthorized("Credenciales incorrectas");
            }
            return Ok(resultado);
        }

        [HttpPost("addrole")]
        public async Task<IActionResult> AddRoleAsync([FromBody] AddRoleDTO addRoleDTO)
        {
            var result = await _userServices.AddRoleAsync(addRoleDTO);
            if (result.Contains("agregado exitosamente"))
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        [HttpPost("refreshtoken")]
        public async Task<IActionResult> RefreshTokenAsync()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var resultado = await _userServices.RefreshTokenAsync(refreshToken);
            if (!string.IsNullOrEmpty(resultado.RefreshToken))
                SetRefreshTokenInCookie(resultado.RefreshToken);
            
            return Ok(resultado);
        }

        private void SetRefreshTokenInCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(10)
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
