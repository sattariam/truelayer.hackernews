using Newtonsoft.Json;

namespace TrueLayer.HackerNews.Models
{
    public class NewsItem
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string By { get; set; }
        public int Descendants { get; set; }
        public string Url { get; set; }
        public int Score { get; set; }
    }
}
