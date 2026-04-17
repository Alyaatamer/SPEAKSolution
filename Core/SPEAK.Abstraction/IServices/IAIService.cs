using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SPEAK.Web.Services
{
    public interface IAIService
    {
        Task<Stream> GetResponseStreamAsync(string message , string SessionId);
    }

}