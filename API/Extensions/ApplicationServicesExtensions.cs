using Core.Interfaces;
using Infrastructure.Repositories;
using Infrastructure.UnitOfWork;

namespace API.Extensions
{
    public static class ApplicationServicesExtensions
    {

        public static void ConfigurationCors(this IServiceCollection services) =>
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
            });

        public static void AddAplicacionServices(this IServiceCollection services)
        {
            //services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            //services.AddScoped<IProductoRepository, ProductoRepository>();
            //services.AddScoped<IMarcaRepository, MarcaRepository>();
            //services.AddScoped<ICategoriaRepository, CategoriaRepository>();  Al usar UnitOfWork ya no es necesario registrar cada repositorio
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }
    }


}
