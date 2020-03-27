using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TrueLayer.HackerNews.Test.Mocks
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private Dictionary<string, string> _expectedResponses;
        private Dictionary<string, HttpStatusCode> _expectedStatusCodes;

        public MockHttpMessageHandler()
        {
            _expectedResponses = new Dictionary<string, string> { { string.Empty, string.Empty } };
            _expectedStatusCodes = new Dictionary<string, HttpStatusCode> { { string.Empty, HttpStatusCode.OK } };
        }

        public string RequestContent { get; private set; }
        public string RequestUrl { get; private set; }
        public int NumberOfCalls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NumberOfCalls++;
            RequestContent = request.Content != null ? await request.Content.ReadAsStringAsync() : string.Empty;
            RequestUrl = request.RequestUri.ToString();
            var response = _expectedResponses.ContainsKey(RequestUrl) ? _expectedResponses[RequestUrl] : _expectedResponses[string.Empty];
            var statusCode = _expectedStatusCodes.ContainsKey(RequestUrl) ? _expectedStatusCodes[RequestUrl] : _expectedStatusCodes[string.Empty];

            return new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content =  new StringContent(response)
            };
        }

        public void SetExpectedResponse(string expectedUrl, string expectedResponse)
        {
            _expectedResponses.TryAdd(expectedUrl, expectedResponse);
        }

        public void SetExpectedStatusCode(HttpStatusCode statusCode)
        {
            _expectedStatusCodes[string.Empty] = statusCode;
        }
        public void SetExpectedStatusCode(string expectedUrl, HttpStatusCode statusCode)
        {
            _expectedStatusCodes.TryAdd(expectedUrl, statusCode);
        }

        public void Reset()
        {
            NumberOfCalls = 0;
            RequestUrl = string.Empty;
            RequestContent = string.Empty;
        }
    }
}
