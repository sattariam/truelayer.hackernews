using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrueLayer.HackerNews.Models;
using TrueLayer.HackerNews.Services;
using TrueLayer.HackerNews.Wrappers;
using Xunit;

namespace TrueLayer.HackerNews.Test.Services
{
    public class ApplicationServiceTests
    {
        private ILogger _logger;
        private INewsService _newsService;
        private IApplicationService _applicationService;
        private IConsoleWrapper _console;

        private void Initialize()
        {
            var loggerFactory = Substitute.For<ILoggerFactory>();
            _logger = Substitute.For<ILogger>();
            loggerFactory.CreateLogger(typeof(NewsService)).Returns(_logger);

            _console = Substitute.For<IConsoleWrapper>();
            _newsService = Substitute.For<INewsService>();
            _applicationService = new ApplicationService(_newsService, loggerFactory, _console);
        }

        private static List<NewsItemOutput> GetNewsItemsSample()
        {
            return new List<NewsItemOutput> {
                new NewsItemOutput
                {
                    Author = "test author 1",
                    Comments = 10,
                    Points = 10,
                    Title = "Test news 1",
                    Uri = "http://www.google.com/"
                },
                new NewsItemOutput
                {
                    Author = "test author 2",
                    Comments = 20,
                    Points = 20,
                    Title = "Test news 2",
                    Uri = "http://www.google2.com/"
                },
                new NewsItemOutput
                {
                    Author = "test author 3",
                    Comments = 30,
                    Points = 30,
                    Title = "Test news 3",
                    Uri = "http://www.google3.com/"
                }
            };
        }

        private static string SerializeObject(object objectData) => JsonConvert.SerializeObject(objectData,
                                        new JsonSerializerSettings
                                        {
                                            ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                            Formatting = Formatting.Indented
                                        });


        [Fact]
        public async Task Run_WhenParametersPassed_ExecuteServiceAndPrintResult()
        {
            // Arrange
            Initialize();
            var expectedNewsItems = GetNewsItemsSample();
            _newsService.GetTopNews(10).Returns(expectedNewsItems);

            // Act
            await _applicationService.Run(new string[] { "--posts", "10" });

            // Assert
            await _newsService.Received(1).GetTopNews(10);
            _console.Received(1).WriteLine("____________________________________________________________________________________");
            _console.Received(1).WriteLine("Output: ");
            _console.Received(1).WriteLine(SerializeObject(expectedNewsItems));
        }

        [Fact]
        public async Task Run_WhenParametersNotPassed_ExecuteServiceAndPrintResult()
        {
            // Arrange
            Initialize();
            var expectedNewsItems = GetNewsItemsSample();
            _newsService.GetTopNews(0).Returns(expectedNewsItems);

            // Act
            await _applicationService.Run(new string[] { });

            // Assert
            await _newsService.Received(1).GetTopNews(0);
            _console.Received(1).WriteLine("____________________________________________________________________________________");
            _console.Received(1).WriteLine("Output: ");
            _console.Received(1).WriteLine(SerializeObject(expectedNewsItems));
        }
    }
}
