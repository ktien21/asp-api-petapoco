using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WEB.SERVICE.PersonService;

namespace WEB.SERVICE
{
    public static class ServiceExtension
    {
        public static IServiceCollection AddService(this IServiceCollection services)
        {
            services.AddScoped<PersonSV>();
            return services;
        }
    }
}
