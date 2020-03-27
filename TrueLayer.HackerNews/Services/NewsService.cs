using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TrueLayer.HackerNews.Models;
using LazyCache;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;

namespace TrueLayer.HackerNews.Services
{
    public class NewsService : INewsService
    {
        private readonly HttpClient _httpClient;
        private readonly IAppCache _cache;
        private readonly ILogger _logger;
        public NewsService(IHttpClientFactory httpClientFactory, IOptions<AppSettings> appsettings, IAppCache cache, ILoggerFactory loggerFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(appsettings.Value.HackerNewsApiUrl);
            _cache = cache;
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public async Task<List<NewsItemOutput>> GetTopNews(int count)
        {
            try
            {
                _logger.LogDebug($"Executing GetTopNews, posts count: {count}.");

                var latestNewsIds = await GetTopNewsIds();

                var newsItems = await GetNewsItemOutputs(latestNewsIds, count);

                _logger.LogDebug($"GetTopNews Execution finished.");

                return newsItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetTopNews Execution failed.");
                return null;
            }

        }

        private async Task<List<NewsItemOutput>> GetNewsItemOutputs(IEnumerable<long> latestNewsIds, int count)
        {
            if(latestNewsIds == null || latestNewsIds.Count() == 0)
            {
                _logger.LogError("Not any news item Ids available.");
                return null;
            }

            _logger.LogDebug($"Getting news items detail started, requested posts count: {count}.");

            var requestedPostsCount = count <= 0 || count > 100 ? 100 : count;
            var postsCount = requestedPostsCount;
            var totalTakenNewsItemIds = 0;

            var newsItems = new List<NewsItemOutput>();

            while (newsItems.Count() != requestedPostsCount && totalTakenNewsItemIds < latestNewsIds.Count())
            {
                var takenNewsItemIds = latestNewsIds.Skip(totalTakenNewsItemIds).Take(postsCount);
                var getNewsItemDetailTasks = takenNewsItemIds
                                .Select(newsItemId => _cache.GetOrAddAsync($"news-id-{newsItemId}", cacheEntry =>
                                {
                                    {
                                        cacheEntry.SlidingExpiration = TimeSpan.FromHours(6);
                                        return GetNewsItemDetail(newsItemId);
                                    }
                                }))
                                .ToList();

                await Task.WhenAll(getNewsItemDetailTasks);

                newsItems.AddRange(getNewsItemDetailTasks
                                    .Where(t => t.Result != null)
                                    .Select(t => t.Result)
                                    .ToList());

                totalTakenNewsItemIds += postsCount;
                postsCount = requestedPostsCount - newsItems.Count();
            }


            if (totalTakenNewsItemIds - newsItems.Count() > 0)
            {
                _logger.LogWarning($"{totalTakenNewsItemIds - newsItems.Count()} News Item ignored due to validation failure.");
            }

            _logger.LogDebug($"Getting news items detail finished.");
            return SetNewsItemsRank(newsItems);
        }

        private static List<NewsItemOutput> SetNewsItemsRank(List<NewsItemOutput> newsItems)
        {
            return newsItems
                .Select((t, index) =>
                                {
                                    var newsItem = t;
                                    newsItem.Rank = index + 1;
                                    return newsItem;
                                })
                            .ToList();
        }

        private async Task<List<long>> GetTopNewsIds()
        {
            _logger.LogDebug($"Getting the latest news item ids started.");

            var newsIdsResult = await _httpClient.GetAsync("topstories.json");

            if (!newsIdsResult.IsSuccessStatusCode)
            {
                _logger.LogError($"Getting latest news ids failed.");
                return null;
            }

            var resultContent = await newsIdsResult.Content.ReadAsStringAsync();

            var newsIds = JsonConvert.DeserializeObject<List<long>>(resultContent, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

            _logger.LogDebug($"Getting the latest news item ids finished.");

            return newsIds.ToList();
        }

        private async Task<NewsItemOutput> GetNewsItemDetail(long newsItemId)
        {
            try
            {
                var newsDetailResult = await _httpClient.GetAsync($"item/{newsItemId}.json");

                if (!newsDetailResult.IsSuccessStatusCode)
                {
                    _logger.LogError($"Getting news item detail failed. NewsItemId: {newsItemId}");
                    return null;
                }

                var newsItem = JsonConvert.DeserializeObject<NewsItem>(await newsDetailResult.Content.ReadAsStringAsync());

                ValidateNewsItem(newsItem, newsItemId);

                return new NewsItemOutput
                {
                    Author = newsItem.By,
                    Title = newsItem.Title,
                    Comments = newsItem.Descendants,
                    Points = newsItem.Score,
                    Uri = newsItem.Url,
                    Rank = 0
                };
            }
            catch (ValidationException ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }

        private void ValidateNewsItem(NewsItem newsItem, long newsItemId)
        {
            if (newsItem == null)
            {
                throw new ValidationException($"News item detail is not available. newsItemId: {newsItemId}");
            }

            if (string.IsNullOrEmpty(newsItem.Title) || newsItem.Title.Length > 256)
            {
                throw new ValidationException($"News item title is not valid. newsItemId: {newsItem.Id}, title: '{newsItem.Title}'");
            }

            if (string.IsNullOrEmpty(newsItem.By) || newsItem.By.Length > 256)
            {
                throw new ValidationException($"News item author is not valid. newsItemId: {newsItem.Id}, author: '{newsItem.By}'");
            }

            if (!Uri.IsWellFormedUriString(newsItem.Url, UriKind.Absolute))
            {
                throw new ValidationException($"News item Uri is not valid. newsItemId: {newsItem.Id}, Uri: '{newsItem.Url}'");
            }

            if (newsItem.Score < 0)
            {
                throw new ValidationException($"News item Points value is not valid. newsItemId: {newsItem.Id}, Points: '{newsItem.Score}'");
            }

            if (newsItem.Descendants < 0)
            {
                throw new ValidationException($"News item Comments value is not valid. newsItemId: {newsItem.Id}, Points: '{newsItem.Descendants}'");
            }
        }
    }
}
