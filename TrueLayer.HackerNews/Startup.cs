using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using TrueLayer.HackerNews.Services;
using TrueLayer.HackerNews.Models;
using TrueLayer.HackerNews.Wrappers;

namespace TrueLayer.HackerNews
{
    public static class Startup
    {
        static Startup()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
        }

        public static IConfiguration Configuration { get; }

        public static ServiceProvider ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddOptions();

            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            services.AddHttpClient();
            services.AddLazyCache();
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddLog4Net("log4net.config");
            });

            services.AddSingleton<IConsoleWrapper, ConsoleWrapper>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<INewsService, NewsService>();

            return services.BuildServiceProvider();
        }
    }
}
