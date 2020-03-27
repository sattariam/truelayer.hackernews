using FluentAssertions;
using LazyCache.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TrueLayer.HackerNews.Services;
using TrueLayer.HackerNews.Models;
using TrueLayer.HackerNews.Test.Mocks;
using Xunit;

namespace TrueLayer.HackerNews.Test.Services
{
    public class NewsServiceTests
    {
        private ILogger _logger;
        private INewsService _newsService;
        private MockHttpMessageHandler _messageHandler;

        private void Initialize()
        {
            var loggerFactory = Substitute.For<ILoggerFactory>();
            _logger = Substitute.For<ILogger>();
            loggerFactory.CreateLogger(typeof(NewsService)).Returns(_logger);

            var mockedCache = new MockCachingService();

            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            _messageHandler = new MockHttpMessageHandler();
            var client = new HttpClient(_messageHandler);
            httpClientFactory.CreateClient().Returns(client);

            _newsService = new NewsService(httpClientFactory, Options.Create(new AppSettings { HackerNewsApiUrl = "http://localhost" }), mockedCache, loggerFactory);

        }

        [Fact]
        public async Task GetTopNews_WhenThereIsNotAnyLatestNewsItemIds_ReturnNullAndLog()
        {
            // Arrange
            Initialize();
            _messageHandler.SetExpectedResponse("http://localhost/topstories.json", JsonConvert.SerializeObject(new List<long>()));
            var expectedCount = 10;

            // Act
            var result = await _newsService.GetTopNews(expectedCount);

            // Assert
            _messageHandler.NumberOfCalls.Should().Be(1);
            result.Should().BeNull();

            _logger.Received(1).LogDebug($"Executing GetTopNews, posts count: {expectedCount}.");
            _logger.Received(1).LogDebug("Getting the latest news item ids started.");
            _logger.Received(1).LogDebug("Getting the latest news item ids finished.");
            _logger.Received(1).LogError("Not any news item Ids available.");
            _logger.Received(1).LogDebug("GetTopNews Execution finished.");
        }

        [Fact]
        public async Task GetTopNews_WhenGetsUnsucceedResponseForLatestNewsIds_ReturnNullAndLog()
        {
            // Arrange
            Initialize();
            _messageHandler.SetExpectedStatusCode(HttpStatusCode.BadRequest);
            var expectedCount = 10;

            // Act
            var result = await _newsService.GetTopNews(expectedCount);

            // Assert
            _messageHandler.NumberOfCalls.Should().Be(1);
            result.Should().BeNull();

            _logger.Received(1).LogDebug($"Executing GetTopNews, posts count: {expectedCount}.");
            _logger.Received(1).LogDebug("Getting the latest news item ids started.");
            _logger.Received(1).LogError("Getting latest news ids failed.");
            _logger.Received(1).LogError("Not any news item Ids available.");
            _logger.Received(1).LogDebug("GetTopNews Execution finished.");
        }

        [Fact]
        public async Task GetTopNews_WhenGetsInvalidValuesForLatestNewsIds_ReturnNullAndLog()
        {
            // Arrange
            Initialize();
            _messageHandler.SetExpectedResponse("http://localhost/topstories.json", JsonConvert.SerializeObject(new List<string> { "invalidId", "11" }));
            var expectedCount = 10;

            // Act
            var result = await _newsService.GetTopNews(expectedCount);

            // Assert
            _messageHandler.NumberOfCalls.Should().Be(1);
            result.Should().BeNull();

            _logger.Received(1).LogDebug($"Executing GetTopNews, posts count: {expectedCount}.");
            _logger.Received(1).LogDebug("Getting the latest news item ids started.");
            _logger.LogError(Arg.Any<JsonSerializationException>(), "GetTopNews Execution failed.");
        }

        [Fact]
        public async Task GetTopNews_WhenNewsItemDetailsAreNotAvailable_ReturnNullAndLog()
        {
            // Arrange
            Initialize();
            _messageHandler.SetExpectedResponse("http://localhost/topstories.json", JsonConvert.SerializeObject(new List<long> { 1, 2, 3, 4, 5 }));
            var expectedCount = 10;

            // Act
            var result = await _newsService.GetTopNews(expectedCount);

            // Assert
            _messageHandler.NumberOfCalls.Should().Be(6);
            result.Should().BeOfType(typeof(List<NewsItemOutput>));
            result.Count().Should().Be(0);

            _logger.Received(1).LogDebug($"Executing GetTopNews, posts count: {expectedCount}.");
            _logger.Received(1).LogDebug("Getting the latest news item ids started.");
            _logger.Received(1).LogDebug("Getting the latest news item ids finished.");
            _logger.LogError("News item detail is not available. newsItemId: 1");
            _logger.LogError("News item detail is not available. newsItemId: 2");
            _logger.LogError("News item detail is not available. newsItemId: 3");
            _logger.LogError("News item detail is not available. newsItemId: 4");
            _logger.LogError("News item detail is not available. newsItemId: 5");
            _logger.Received(1).LogDebug("GetTopNews Execution finished.");
        }

        [Fact]
        public async Task GetTopNews_WhenGetsUnSucceedTheNewsItemDetailsAreNotAvailable_ReturnNullAndLog()
        {
            // Arrange
            Initialize();
            _messageHandler.SetExpectedResponse("http://localhost/topstories.json", JsonConvert.SerializeObject(new List<long> { 1, 2, 3, 4, 5 }));
            _messageHandler.SetExpectedStatusCode("http://localhost/item/2.json", HttpStatusCode.BadRequest);
            _messageHandler.SetExpectedStatusCode("http://localhost/item/4.json", HttpStatusCode.BadRequest);
            var expectedCount = 10;

            // Act
            var result = await _newsService.GetTopNews(expectedCount);

            // Assert
            _messageHandler.NumberOfCalls.Should().Be(6);
            result.Should().BeOfType(typeof(List<NewsItemOutput>));
            result.Count().Should().Be(0);

            _logger.Received(1).LogDebug($"Executing GetTopNews, posts count: {expectedCount}.");
            _logger.Received(1).LogDebug("Getting the latest news item ids started.");
            _logger.Received(1).LogDebug("Getting the latest news item ids finished.");
            _logger.LogError("News item detail is not available. newsItemId: 1");
            _logger.LogError("Getting news item detail failed. NewsItemId:  2");
            _logger.LogError("News item detail is not available. newsItemId: 3");
            _logger.LogError("Getting news item detail failed. NewsItemId:  4");
            _logger.LogError("News item detail is not available. newsItemId: 5");
            _logger.Received(1).LogDebug("GetTopNews Execution finished.");
        }

        [Fact]
        public async Task GetTopNews_WhenNewsItemsHaveValidDetail_ReturnNewsItemsOutputAndLog()
        {
            // Arrange
            Initialize();
            _messageHandler.SetExpectedResponse("http://localhost/topstories.json", JsonConvert.SerializeObject(new List<long> { 3, 2, 1 }));
            var expectedItems = new List<NewsItem> {
                new NewsItem {
                    By = "test author 1",
                    Descendants = 10,
                    Id = 3,
                    Score = 10,
                    Title = "Test news 1",
                    Url = "http://www.google.com/"
                },
                new NewsItem
                {
                    By = "test author 2",
                    Descendants = 20,
                    Id = 2,
                    Score = 20,
                    Title = "Test news 2",
                    Url = "http://www.google.com/"
                },
                new NewsItem
                {
                    By = "test author 3",
                    Descendants = 30,
                    Id = 1,
                    Score = 30,
                    Title = "Test news 3",
                    Url = "http://www.google.com/"
                }
            };
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[0].Id}.json", JsonConvert.SerializeObject(expectedItems[0]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[1].Id}.json", JsonConvert.SerializeObject(expectedItems[1]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[2].Id}.json", JsonConvert.SerializeObject(expectedItems[2]));

            var expectedCount = 3;

            // Act
            var result = await _newsService.GetTopNews(expectedCount);

            // Assert
            _messageHandler.NumberOfCalls.Should().Be(4);
            result.Should().BeOfType(typeof(List<NewsItemOutput>));
            result.Count().Should().Be(3);

            for (int i = 0; i < expectedCount; i++)
            {
                result[i].Title.Should().Be(expectedItems[i].Title);
                result[i].Uri.Should().Be(expectedItems[i].Url);
                result[i].Author.Should().Be(expectedItems[i].By);
                result[i].Points.Should().Be(expectedItems[i].Score);
                result[i].Comments.Should().Be(expectedItems[i].Descendants);
                result[i].Rank.Should().Be(i + 1);
            }

            _logger.Received(1).LogDebug($"Executing GetTopNews, posts count: {expectedCount}.");
            _logger.Received(1).LogDebug("Getting the latest news item ids started.");
            _logger.Received(1).LogDebug("Getting the latest news item ids finished.");
            _logger.Received(1).LogDebug("GetTopNews Execution finished.");
        }

        [Fact]
        public async Task GetTopNews_WhenNewsItemsHaveInvalidDetail_ReturnNewsItemsOutputAndLog()
        {
            // Arrange
            Initialize();
            _messageHandler.SetExpectedResponse("http://localhost/topstories.json", JsonConvert.SerializeObject(new List<long> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
            var expectedItems = new List<NewsItem> {
                new NewsItem // Null Url
                {
                    By = "test author 9",
                    Descendants = 90,
                    Id = 9,
                    Score = 90,
                    Title = "Test news 9",
                    Url = null
                },
                new NewsItem { // Invalid Descendants
                    By = "test author 1",
                    Descendants = -10,
                    Id = 8,
                    Score = 10,
                    Title = "Test news 1",
                    Url = "http://www.google.com/"
                },
                new NewsItem // Invalid Score
                {
                    By = "test author 2",
                    Descendants = 20,
                    Id = 7,
                    Score = -20,
                    Title = "Test news 2",
                    Url = "http://www.google.com/"
                },
                new NewsItem // Null Author (By)
                {
                    By = null,
                    Descendants = 30,
                    Id = 6,
                    Score = 30,
                    Title = "Test news 3",
                    Url = "http://www.google.com/"
                },
                new NewsItem // Null Title
                {
                    By = "test author 4",
                    Descendants = 40,
                    Id = 5,
                    Score = 40,
                    Title = null,
                    Url = "http://www.google.com/"
                },
                new NewsItem // Invalid Url
                {
                    By = "test author 5",
                    Descendants = 50,
                    Id = 4,
                    Score = 50,
                    Title = "Test news 5",
                    Url = "////.._pwwwgooglecom"
                },
                new NewsItem // Long Title 
                {
                    By = "test author 6",
                    Descendants = 60,
                    Id = 3,
                    Score = 60,
                    Title = "Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6 Test news 6",
                    Url = "http://www.google.com/"
                },
                new NewsItem // Long author name
                {
                    By = "test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7",
                    Descendants = 70,
                    Id = 2,
                    Score = 70,
                    Title = "Test news 7",
                    Url = "http://www.google.com/"
                },
                new NewsItem // Valid 
                {
                    By = "test author 8",
                    Descendants = 80,
                    Id = 1,
                    Score = 80,
                    Title = "Test news 8",
                    Url = "http://www.google.com/"
                }
            };
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[0].Id}.json", JsonConvert.SerializeObject(expectedItems[0]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[1].Id}.json", JsonConvert.SerializeObject(expectedItems[1]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[2].Id}.json", JsonConvert.SerializeObject(expectedItems[2]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[3].Id}.json", JsonConvert.SerializeObject(expectedItems[3]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[4].Id}.json", JsonConvert.SerializeObject(expectedItems[4]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[5].Id}.json", JsonConvert.SerializeObject(expectedItems[5]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[6].Id}.json", JsonConvert.SerializeObject(expectedItems[6]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[7].Id}.json", JsonConvert.SerializeObject(expectedItems[7]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[8].Id}.json", JsonConvert.SerializeObject(expectedItems[8]));

            var expectedCount = 9;

            // Act
            var result = await _newsService.GetTopNews(expectedCount);

            // Assert
            _messageHandler.NumberOfCalls.Should().Be(10);
            result.Should().BeOfType(typeof(List<NewsItemOutput>));
            result.Count().Should().Be(1);

            result[0].Title.Should().Be(expectedItems[8].Title);
            result[0].Uri.Should().Be(expectedItems[8].Url);
            result[0].Author.Should().Be(expectedItems[8].By);
            result[0].Points.Should().Be(expectedItems[8].Score);
            result[0].Comments.Should().Be(expectedItems[8].Descendants);
            result[0].Rank.Should().Be(1);

            _logger.Received(1).LogDebug($"Executing GetTopNews, posts count: {expectedCount}.");
            _logger.Received(1).LogDebug("Getting the latest news item ids started.");
            _logger.Received(1).LogDebug("Getting the latest news item ids finished.");
            _logger.Received(1).LogError($"News item title is not valid. newsItemId: {expectedItems[6].Id}, title: '{expectedItems[6].Title}'");
            _logger.Received(1).LogError($"News item title is not valid. newsItemId: {expectedItems[4].Id}, title: '{expectedItems[4].Title}'");
            _logger.Received(1).LogError($"News item author is not valid. newsItemId: {expectedItems[3].Id}, author: '{expectedItems[3].By}'");
            _logger.Received(1).LogError($"News item author is not valid. newsItemId: {expectedItems[7].Id}, author: '{expectedItems[7].By}'");
            _logger.Received(1).LogError($"News item Uri is not valid. newsItemId: {expectedItems[5].Id}, Uri: '{expectedItems[5].Url}'");
            _logger.Received(1).LogError($"News item Uri is not valid. newsItemId: {expectedItems[0].Id}, Uri: '{expectedItems[0].Url}'");
            _logger.Received(1).LogError($"News item Points value is not valid. newsItemId: {expectedItems[2].Id}, Points: '{expectedItems[2].Score}'");
            _logger.Received(1).LogError($"News item Comments value is not valid. newsItemId: {expectedItems[1].Id}, Points: '{expectedItems[1].Descendants}'");
            _logger.Received(1).LogWarning($"8 News Item ignored due to validation failure.");
            _logger.Received(1).LogDebug("GetTopNews Execution finished.");
        }

        [Fact]
        public async Task GetTopNews_WhenSomeOfNewsItemsHaveInvalidDetail_ReturnNewsItemsOutputAndLog()
        {
            // Arrange
            Initialize();
            _messageHandler.SetExpectedResponse("http://localhost/topstories.json", JsonConvert.SerializeObject(new List<long> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
            var expectedItems = new List<NewsItem> {
                new NewsItem
                {
                    By = "test author 9",
                    Descendants = 90,
                    Id = 9,
                    Score = 90,
                    Title = "Test news 9",
                    Url = "http://www.test.com/"
                },
                new NewsItem { // Invalid Descendants
                    By = "test author 1",
                    Descendants = -10,
                    Id = 8,
                    Score = 10,
                    Title = "Test news 1",
                    Url = "http://www.test.com/"
                },
                new NewsItem
                {
                    By = "test author 2",
                    Descendants = 20,
                    Id = 7,
                    Score = 20,
                    Title = "Test news 2",
                    Url = "http://www.test.com/"
                },
                new NewsItem
                {
                    By = "Title test 3",
                    Descendants = 30,
                    Id = 6,
                    Score = 30,
                    Title = "Test news 3",
                    Url = "http://www.test.com/"
                },
                new NewsItem // Null Title
                {
                    By = "test author 4",
                    Descendants = 40,
                    Id = 5,
                    Score = 40,
                    Title = null,
                    Url = "http://www.test.com/"
                },
                new NewsItem // Invalid Url
                {
                    By = "test author 5",
                    Descendants = 50,
                    Id = 4,
                    Score = 50,
                    Title = "Test news 5",
                    Url = "////.._pwwwgooglecom"
                },
                new NewsItem
                {
                    By = "test author 6",
                    Descendants = 60,
                    Id = 3,
                    Score = 60,
                    Title = "Test news 6",
                    Url = "http://www.test.com/"
                },
                new NewsItem // Long author name
                {
                    By = "test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7 test author 7",
                    Descendants = 70,
                    Id = 2,
                    Score = 70,
                    Title = "Test news 7",
                    Url = "http://www.test.com/"
                },
                new NewsItem
                {
                    By = "test author 8",
                    Descendants = 80,
                    Id = 1,
                    Score = 80,
                    Title = "Test news 8",
                    Url = "http://www.test.com/"
                }
            };
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[0].Id}.json", JsonConvert.SerializeObject(expectedItems[0]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[1].Id}.json", JsonConvert.SerializeObject(expectedItems[1]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[2].Id}.json", JsonConvert.SerializeObject(expectedItems[2]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[3].Id}.json", JsonConvert.SerializeObject(expectedItems[3]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[4].Id}.json", JsonConvert.SerializeObject(expectedItems[4]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[5].Id}.json", JsonConvert.SerializeObject(expectedItems[5]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[6].Id}.json", JsonConvert.SerializeObject(expectedItems[6]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[7].Id}.json", JsonConvert.SerializeObject(expectedItems[7]));
            _messageHandler.SetExpectedResponse($"http://localhost/item/{expectedItems[8].Id}.json", JsonConvert.SerializeObject(expectedItems[8]));

            var expectedCount = 5;

            // Act
            var result = await _newsService.GetTopNews(expectedCount);

            // Assert
            _messageHandler.NumberOfCalls.Should().Be(10);
            result.Should().BeOfType(typeof(List<NewsItemOutput>));
            result.Count().Should().Be(5);

            result[0].Title.Should().Be(expectedItems[8].Title);
            result[0].Uri.Should().Be(expectedItems[8].Url);
            result[0].Author.Should().Be(expectedItems[8].By);
            result[0].Points.Should().Be(expectedItems[8].Score);
            result[0].Comments.Should().Be(expectedItems[8].Descendants);
            result[0].Rank.Should().Be(1);

            result[1].Title.Should().Be(expectedItems[6].Title);
            result[1].Uri.Should().Be(expectedItems[6].Url);
            result[1].Author.Should().Be(expectedItems[6].By);
            result[1].Points.Should().Be(expectedItems[6].Score);
            result[1].Comments.Should().Be(expectedItems[6].Descendants);
            result[1].Rank.Should().Be(2);

            result[2].Title.Should().Be(expectedItems[3].Title);
            result[2].Uri.Should().Be(expectedItems[3].Url);
            result[2].Author.Should().Be(expectedItems[3].By);
            result[2].Points.Should().Be(expectedItems[3].Score);
            result[2].Comments.Should().Be(expectedItems[3].Descendants);
            result[2].Rank.Should().Be(3);

            result[3].Title.Should().Be(expectedItems[2].Title);
            result[3].Uri.Should().Be(expectedItems[2].Url);
            result[3].Author.Should().Be(expectedItems[2].By);
            result[3].Points.Should().Be(expectedItems[2].Score);
            result[3].Comments.Should().Be(expectedItems[2].Descendants);
            result[3].Rank.Should().Be(4);

            result[4].Title.Should().Be(expectedItems[0].Title);
            result[4].Uri.Should().Be(expectedItems[0].Url);
            result[4].Author.Should().Be(expectedItems[0].By);
            result[4].Points.Should().Be(expectedItems[0].Score);
            result[4].Comments.Should().Be(expectedItems[0].Descendants);
            result[4].Rank.Should().Be(5);

            _logger.Received(1).LogDebug($"Executing GetTopNews, posts count: {expectedCount}.");
            _logger.Received(1).LogDebug("Getting the latest news item ids started.");
            _logger.Received(1).LogDebug("Getting the latest news item ids finished.");
            _logger.Received(1).LogError($"News item title is not valid. newsItemId: {expectedItems[4].Id}, title: '{expectedItems[4].Title}'");
            _logger.Received(1).LogError($"News item author is not valid. newsItemId: {expectedItems[7].Id}, author: '{expectedItems[7].By}'");
            _logger.Received(1).LogError($"News item Uri is not valid. newsItemId: {expectedItems[5].Id}, Uri: '{expectedItems[5].Url}'");
            _logger.Received(1).LogError($"News item Comments value is not valid. newsItemId: {expectedItems[1].Id}, Points: '{expectedItems[1].Descendants}'");
            _logger.Received(1).LogWarning($"4 News Item ignored due to validation failure.");
            _logger.Received(1).LogDebug("GetTopNews Execution finished.");
        }
    }
}
