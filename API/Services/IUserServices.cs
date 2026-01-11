using API.DTO;

namespace API.Services
{
    public interface IUserServices
    {
        Task<string> RegisterAsync(RegisterDTO model);
        Task<DatosUsuarioDTO> GetTokenAsync(LoginDTO model);
        Task<string> AddRoleAsync(AddRoleDTO model);
    }
}
