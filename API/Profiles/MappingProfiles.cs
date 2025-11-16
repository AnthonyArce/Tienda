using API.DTO;
using AutoMapper;
using Core.Entities;

namespace API.Profiles
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles() 
        {
            CreateMap<Producto, ProductoDTO>().ReverseMap();
            CreateMap<Marca, MarcaDTO>().ReverseMap();
            CreateMap<Categoria, CategoriaDTO>().ReverseMap();  
        }        
    
    }
}
