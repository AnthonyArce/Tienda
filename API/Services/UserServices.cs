using API.DTO;
using API.Helpers;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace API.Services
{
    public class UserServices : IUserServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly JWT _jwt;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        public UserServices(IUnitOfWork unitOfWork, IOptions<JWT> jwt, IPasswordHasher<Usuario> passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _jwt = jwt.Value;
            _passwordHasher = passwordHasher;
        }

        public async Task<string> RegisterAsync(RegisterDTO registerDTO)
        {
            try
            {
                var userExiste = _unitOfWork.Usuarios
                                    .Find(u => u.Email.ToLower() == registerDTO.Email.ToLower())
                                    .FirstOrDefault();

                if (userExiste != null)
                {
                    return $"El email {registerDTO.Email} ya se encuentra registrado";
                }

                var usuario = new Usuario
                {
                    Nombre = registerDTO.Nombres,
                    ApellidoPaterno = registerDTO.ApellidoPaterno,
                    ApellidoMaterno = registerDTO.ApellidoMaterno,
                    Email = registerDTO.Email,
                    Username = registerDTO.Username
                };

                usuario.Password = _passwordHasher.HashPassword(usuario, registerDTO.Password);

                var rolPredeterminado = _unitOfWork.Roles
                                                .Find(r => r.Nombre == Autorizacion.rol_predeterminado.ToString())
                                                .FirstOrDefault();

                usuario.Roles.Add(rolPredeterminado);
                _unitOfWork.Usuarios.Add(usuario);

                await _unitOfWork.SaveAsync();

                return $"El usuario {registerDTO.Username} ha sido registrado exitosamente";
            }
            catch (Exception ex)
            {
                return $"Error al registrar el usuario {registerDTO.Username}: {ex.Message}";

            }
        }

        public async Task<DatosUsuarioDTO> GetTokenAsync(LoginDTO loginDTO)
        {
            try
            {
                DatosUsuarioDTO datosUsuarioDTO = new DatosUsuarioDTO();
                var usuario = await _unitOfWork.Usuarios.GetByUsernameAsync(loginDTO.Username);


                if (usuario == null)
                {
                    datosUsuarioDTO.EstaAutenticado = false;
                    datosUsuarioDTO.Mensaje = $"El usuario {loginDTO.Username} no existe";
                    return datosUsuarioDTO;
                }

                var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.Password, loginDTO.Password);

                if (resultado == PasswordVerificationResult.Success)
                {
                    datosUsuarioDTO.EstaAutenticado = true;
                    JwtSecurityToken jwtSecurityToken = CreateJwtToken(usuario);
                    datosUsuarioDTO.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
                    datosUsuarioDTO.Mensaje = $"El usuario {loginDTO.Username} ha sido autenticado exitosamente";
                    datosUsuarioDTO.UserName = usuario.Username;
                    datosUsuarioDTO.Email = usuario.Email;
                    datosUsuarioDTO.Roles = usuario.Roles.Select(u => u.Nombre).ToList();

                    return datosUsuarioDTO;
                }
                else
                {
                    datosUsuarioDTO.EstaAutenticado = false;
                    datosUsuarioDTO.Mensaje = $"Las credenciales del usuario {loginDTO.Username} son incorrectas";
                    return datosUsuarioDTO;
                }

            }
            catch (Exception ex)
            {
                return new DatosUsuarioDTO
                {
                    Mensaje = $"Error al autenticar el usuario {loginDTO.Username}: {ex.Message}",
                    EstaAutenticado = false
                };
            }
        }

        public async Task<string> AddRoleAsync(AddRoleDTO addRoleDTO)
        {
            var usuario =await _unitOfWork.Usuarios.GetByUsernameAsync(addRoleDTO.UserName);

            if (usuario == null)
                return $"El usuario {addRoleDTO.UserName} no existe";

            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.Password, addRoleDTO.Password);

            if (resultado == PasswordVerificationResult.Success)
            { 

                var RolExiste = _unitOfWork.Roles
                                    .Find(r => r.Nombre.ToLower() == addRoleDTO.Role.ToLower())
                                    .FirstOrDefault();

                if(RolExiste!=null)
                {
                    var usuarioTieneRol = usuario.Roles.Any(r => r.Id == RolExiste.Id);

                    if(!usuarioTieneRol)
                    {
                        usuario.Roles.Add(RolExiste);
                        _unitOfWork.Usuarios.Update(usuario);
                        await _unitOfWork.SaveAsync();
                    }

                    return $"Rol {addRoleDTO.Role} agregado a la cuenta {addRoleDTO.UserName} de forma exitosa";
                    
                }
                return $"El rol {addRoleDTO.Role} no existe";
            }
            return $"Las credenciales del usuario {addRoleDTO.UserName} son incorrectas";
        }

        public JwtSecurityToken CreateJwtToken(Usuario usuario)
        {
            var roles = usuario.Roles;
            var roleClaims = new List<Claim>();

            foreach (var rol in roles)
            {
                roleClaims.Add(new Claim(ClaimTypes.Role, rol.Nombre));
            }
            var claims = new List<Claim>
            {
               new Claim(JwtRegisteredClaimNames.Sub, usuario.Username),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
               new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
               new Claim("uid", usuario.Id.ToString())

            }.Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes),
                signingCredentials: signingCredentials
                );
            return jwtSecurityToken;
        }
    }
}
