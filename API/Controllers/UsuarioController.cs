using API.DTO;
using API.Services;
using Asp.Versioning;
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
    }
}
