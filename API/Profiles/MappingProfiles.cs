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

            //Mapping for ProductoListDTO

            CreateMap<Producto, ProductoListDTO>()
                .ForMember(dest => dest.Marca, origen => origen.MapFrom(origen => origen.Marca.Nombre))
                .ForMember(dest => dest.Categoria, origen => origen.MapFrom(origen => origen.Categoria.Nombre))
                .ReverseMap()
                .ForMember(dest => dest.Marca, origen => origen.Ignore())
                .ForMember(dest => dest.Categoria, origen => origen.Ignore());

            CreateMap<Producto, ProductoAddUpdateDTO>()
                .ReverseMap()
                .ForMember(dest => dest.Marca, origen => origen.Ignore())
                .ForMember(dest => dest.Categoria, origen => origen.Ignore());
        }

    }
}
