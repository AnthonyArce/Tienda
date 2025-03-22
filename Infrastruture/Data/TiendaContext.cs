using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Infrastruture.Data
{
    public class TiendaContext:DbContext
    {
        public TiendaContext(DbContextOptions<TiendaContext> options) : base(options) 
        {

        }

        public DbSet<Producto> Producto { get; set; }
        public DbSet<Categoria> categoria { get; set; }
        public DbSet<Marca> Marca { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
