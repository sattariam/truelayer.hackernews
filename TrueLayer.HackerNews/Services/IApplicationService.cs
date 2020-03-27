using System.Threading.Tasks;

namespace TrueLayer.HackerNews.Services
{
    public interface IApplicationService
    {
        Task Run(string[] args);
    }
}