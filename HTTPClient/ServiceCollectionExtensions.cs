using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HTTPClient
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCFHttpClient(
            this IServiceCollection services)
        {
            services.AddHttpClient(); // default named client
            services.AddTransient<CFHttpClient>();
            return services;
        }
    }
}
