using Core.Entities;

namespace Core.Interfaces
{
    public interface IUsuarioRepository:IGenericRepository<Usuario>
    {
        public Task<Usuario> GetByUsernameAsync(string username);
    }
}
