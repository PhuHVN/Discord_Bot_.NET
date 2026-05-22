using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyDiscordApp.Application.Interface;

namespace MyDiscordApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register infrastructure services here
            services.AddScoped<ISeedService, SeedService>();
            services.AddHttpClient<IMemeService, MemeService>();
            services.AddHttpClient<GeminiService>();
            services.AddScoped<IGeminiService, GeminiService>();
            services.AddScoped<IHandleTalkService, HandleTalkService>();

            return services;
        }
    }
}

