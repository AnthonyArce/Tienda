using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.configuration
{
    public class ProductoConfiguration: IEntityTypeConfiguration<Producto>
    {
        //RBuqNWMbMZI
        public void Configure(EntityTypeBuilder<Producto> builder)
        {
            builder.ToTable("Productos");
            builder.Property(p => p.Id)
                .IsRequired();
            builder.Property(p =>p.Nombre)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(p => p.Precio)
                .HasColumnType("decimal(18,2)");
            builder.HasOne(p => p.Marca)
                .WithMany(p => p.productos)
                .HasForeignKey(p => p.MarcaId);
            builder.HasOne(p => p.Categoria)
                .WithMany(p => p.productos)
                .HasForeignKey(p => p.CategoriaId);
        }
    }
}
