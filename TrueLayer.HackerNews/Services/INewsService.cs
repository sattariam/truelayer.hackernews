using System.Collections.Generic;
using System.Threading.Tasks;
using TrueLayer.HackerNews.Models;

namespace TrueLayer.HackerNews.Services
{
    public interface INewsService
    {
        Task<List<NewsItemOutput>> GetTopNews(int count);
    }
}