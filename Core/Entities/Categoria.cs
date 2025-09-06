using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class Categoria: BaseEntity
    {        
        public string Nombre { get; set; }
        public ICollection<Producto> productos { get; set; }
    }
}
