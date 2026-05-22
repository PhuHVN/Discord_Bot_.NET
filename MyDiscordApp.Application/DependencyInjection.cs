using Microsoft.Extensions.DependencyInjection;

namespace MyDiscordApp.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register application services here
            services.AddScoped<ITalkService, RandomTalkService>();
            services.AddScoped<IHandleTalkService, HandleTalkService>();
            return services;
        }
    }
}
