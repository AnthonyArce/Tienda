using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
