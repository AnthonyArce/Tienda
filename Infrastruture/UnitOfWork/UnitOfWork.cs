using Core.Interfaces;
using Infrastructure.Repositories;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly TiendaContext _context;
        private IProductoRepository _productoRepository;
        private IMarcaRepository _marcaRepository;
        private ICategoriaRepository _categoriaRepository;


        public UnitOfWork(TiendaContext context)
        {
            _context = context;
        }

        public IProductoRepository Productos => _productoRepository ??= new ProductoRepository(_context);
        public IMarcaRepository Marcas => _marcaRepository ??= new MarcaRepository(_context);
        public ICategoriaRepository Categorias => _categoriaRepository ??= new CategoriaRepository(_context);
        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
