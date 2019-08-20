using System.Net.Http;

namespace MiniRestSharpCore
{
    /// <summary>
    /// The factory implementation is registered against IServiceCollection as a singleton, see
    /// <see cref="Microsoft.Extensions.DependencyInjection.RestSharpServiceCollectionExtensions.AddRestSharp(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>
    /// Implement this interface to provide your own implementation of <see cref="IHttp"/>.
    /// </summary>
    public interface IHttpFactory
    {
        /// <summary>
        /// This method is called per HTTP request (i.e. when <see cref="IRestClient.ExecuteTaskAsync(IRestRequest)"/>
        /// is called). The caller will pass in the named <see cref="HttpClient"/> name used if
        /// <see cref="IRestRequest.UseNamedHttpClient(IHttpClientFactory, string)"/> was called for the request.
        /// </summary>
        /// <param name="name">
        /// Name of the named <see cref="HttpClient"/> used. May be null to indicate no named client used.
        /// </param>
        IHttp Create(string name);

        /// <summary>
        /// Informs the factory that the given <see cref="HttpClientHandler"/> is associated with a named
        /// <see cref="HttpClient"/> by the <paramref name="name"/>. This method is likely called very early on,
        /// perhaps multiple times over the life-time of this factory, possibly before <see cref="Create(string)"/> is called.
        /// </summary>
        void AddNamedClientHandler(string name, HttpClientHandler httpClientHandler);
    }
}
