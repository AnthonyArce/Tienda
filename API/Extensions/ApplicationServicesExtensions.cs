using Asp.Versioning;
using AspNetCoreRateLimit;
using Core.Interfaces;
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
            //ddddddd;  Al usar UnitOfWork ya no es necesario registrar cada repositorio
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }


        public static void ConfigureRateLimitingOptions(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddInMemoryRateLimiting();

            services.Configure<IpRateLimitOptions>(option => 
            {
                option.EnableEndpointRateLimiting = true;
                option.StackBlockedRequests = false;
                option.HttpStatusCode = 429;
                option.RealIpHeader = "X-Real-IP";
                option.GeneralRules = new List<RateLimitRule>
                {
                    new RateLimitRule
                    {
                        Endpoint = "*",
                        Period = "10s",
                        Limit = 2
                    }
                };
            });

        }

        public static void ConfigureApiVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(0, 1);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = new HeaderApiVersionReader("X-Version");
                //options.ApiVersionReader = ApiVersionReader.Combine(   //Sirve para convinar varios métodos de versionamiento
                //    new QueryStringApiVersionReader("v"),
                //    new HeaderApiVersionReader("X-Version")                  
                //);
                options.ReportApiVersions = true;

            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });
        }
    }


}
