using FluentAssertions;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using TrueLayer.HackerNews.Models;
using TrueLayer.HackerNews.Services;
using TrueLayer.HackerNews.Wrappers;
using Xunit;

namespace TrueLayer.HackerNews.Test
{
    public class StartupTests
    {
        [Fact]
        public void ConfigureServices_WhenInvoke_RegistersDependenciesAndSettings()
        {
            //  Arrange & Act
            var serviceProvider = new Startup("testappsettings.json").ConfigureServices();
            using var scope = serviceProvider.CreateScope();

            //  Assert
            scope.Should().NotBeNull();
            var scopeServiceProvider = scope.ServiceProvider;

            scopeServiceProvider.GetRequiredService<INewsService>().Should().NotBeNull();
            scopeServiceProvider.GetRequiredService<IApplicationService>().Should().NotBeNull();
            scopeServiceProvider.GetRequiredService<ILoggerFactory>().Should().NotBeNull();
            var appSettings = scopeServiceProvider.GetRequiredService<IOptions<AppSettings>>();
            appSettings.Should().NotBeNull();
            appSettings.Value.HackerNewsApiUrl.Should().Be("https://hacker-news.firebaseio.com/v0/");
            scopeServiceProvider.GetRequiredService<IAppCache>().Should().NotBeNull();
            scopeServiceProvider.GetRequiredService<IHttpClientFactory>().Should().NotBeNull();
            scopeServiceProvider.GetRequiredService<IConsoleWrapper>().Should().NotBeNull();
        }
    }
}
