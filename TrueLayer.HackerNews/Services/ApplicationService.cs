using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Threading.Tasks;
using TrueLayer.HackerNews.Wrappers;

namespace TrueLayer.HackerNews.Services
{
    public class ApplicationService: IApplicationService
    {
        private readonly INewsService _newsService;
        private readonly ILogger _logger;
        private readonly IConsoleWrapper _console;
        public ApplicationService(INewsService newsService, ILoggerFactory loggerFactory, IConsoleWrapper console)
        {
            _newsService = newsService;
            _logger = loggerFactory.CreateLogger(GetType());
            _console = console;
        }
        public async Task Run(string[] args)
        {
            _logger.LogInformation("Starting Application.");

            var postsCount = GetRequestedPostsCount(args);

            await ExtractNewsItems(postsCount);

            _logger.LogInformation("All done!");
        }

        private async Task ExtractNewsItems(int postsCount)
        {
            var newsItems = await _newsService.GetTopNews(postsCount);

            _console.WriteLine("____________________________________________________________________________________");
            _console.WriteLine("Output: ");
            _console.WriteLine(SerializeObject(newsItems));
        }

        private static string SerializeObject(object objectData)
        {
            return JsonConvert.SerializeObject(objectData,
                                                new JsonSerializerSettings
                                                {
                                                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                                    Formatting = Formatting.Indented
                                                });
        }

        private int GetRequestedPostsCount(string[] args)
        {
            var postsCount = 0;

            if (args.Length > 1 && args[0] == "--posts")
            {
                Int32.TryParse(args[1], out postsCount);
            }

            return postsCount;
        }
    }
}
