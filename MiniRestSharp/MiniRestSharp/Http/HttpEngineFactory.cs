using System;
using System.Net.Http;

namespace MiniRestSharpCore.Http
{
    /// <summary>
    /// <para>
    /// By default, an instance of this class is created for every instance of <see cref="RestClient"/>.
    /// Since the underlying HTTP engine used by <see cref="NetStd20HttpEngine"/> is <see cref="HttpClient"/>,
    /// and we should not return a new HttpClient for every request (<see cref="Create"/> method is called
    /// per request), it seems appropriate to share a single HttpClient across all requests processed by
    /// this object (i.e. a single RestClient).
    /// </para>
    /// <para>
    /// See the following articles for evidence that we should not Dispose of HttpClients after each request
    /// and share them instead.
    /// https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
    /// https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
    /// https://stackoverflow.com/questions/15705092/do-httpclient-and-httpclienthandler-have-to-be-disposed
    /// </para>
    /// </summary>
    internal class HttpEngineFactory : IHttpFactory
    {
        /// <summary>
        /// The single HttpClient shared across all calls to Create().
        /// </summary>
        private HttpClient _httpClient;
        private HttpClientHandler _httpClientHandler;


        public HttpEngineFactory()
        {
            _httpClientHandler = new HttpClientHandler();
            _httpClient = new HttpClient(_httpClientHandler);
        }


        public IHttp Create()
        {
            return new SharedHttpClientHttpEngine(_httpClient, _httpClientHandler);
        }


        private class SharedHttpClientHttpEngine : NetStd20HttpEngine
        {
            private HttpClient _sharedHttpClient;
            private HttpClientHandler _sharedHttpClientHandler;

            public SharedHttpClientHttpEngine(HttpClient sharedClient, HttpClientHandler sharedHandler)
            {
                _sharedHttpClient = sharedClient;
                _sharedHttpClientHandler = sharedHandler;
            }

            protected override NetStd20HttpRequest CreateWebRequest(string method, Uri url)
            {
                return new SharedHttpClientHttpRequest(_sharedHttpClient, _sharedHttpClientHandler, method, url);
            }
        }


        private class SharedHttpClientHttpRequest : NetStd20HttpRequest
        {
            public SharedHttpClientHttpRequest(HttpClient sharedClient, HttpClientHandler sharedHandler, string method, Uri url)
            {
                this.RequestClient = sharedClient;
                this.RequestHandler = sharedHandler;
                this.RequestMessage = new HttpRequestMessage(new HttpMethod(method), url);

                // Must set a HttpContent here to allow HttpContentHeader to be set.
                this.RequestMessage.Content = new ByteArrayContent(new byte[0]);
            }
        }

    }
}
