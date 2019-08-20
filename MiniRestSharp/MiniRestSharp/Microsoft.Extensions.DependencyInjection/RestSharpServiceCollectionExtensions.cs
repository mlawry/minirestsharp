using MiniRestSharpCore;
using MiniRestSharpCore.Http;
using System;
using System.Net.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions methods to configure an Microsoft.Extensions.DependencyInjection.IServiceCollection
    ///     for System.Net.Http.IHttpClientFactory.
    /// </summary>
    public static class RestSharpServiceCollectionExtensions
    {
        /// <summary>
        /// Set up the MiniRestSharp library to work with named clients (and may be typed clients later on as well).
        /// This method also calls <see cref="HttpClientFactoryServiceCollectionExtensions.AddHttpClient(IServiceCollection)"/>.
        /// </summary>
        public static IServiceCollection AddRestSharp(this IServiceCollection services)
        {
            services.AddHttpClient();
            return services.AddSingleton<IHttpFactory, HttpEngineFactory>();
        }


        /// <summary>
        /// You must use this method to add a named <see cref="HttpClient"/> if you want to use the named client
        /// with the MiniRestSharp library.
        /// </summary>
        public static IHttpClientBuilder AddRestSharpClient(this IServiceCollection services, string name)
        {
            return services.AddHttpClient(name).ConfigurePrimaryHttpMessageHandler((IServiceProvider provider) =>
            {
                // We know this works because we added it as a singleton in the method above.
                var httpEngine = provider.GetService<IHttpFactory>() as HttpEngineFactory;
                if (httpEngine == null)
                {
                    throw new InvalidOperationException("You must call AddRestSharp(this IServiceCollection services) before adding a named HttpClient via AddRestSharpClient(this IServiceCollection services, string name).");
                }

                var httpClientHandler = new HttpClientHandler();
                httpEngine.AddNamedClientHandler(name, httpClientHandler);
                return httpClientHandler;
            });
        }


        //public static IHttpClientBuilder AddHttpClient(this IServiceCollection services, string name, Action<HttpClient> configureClient)
        //{

        //}


        //public static IHttpClientBuilder AddHttpClient(this IServiceCollection services, string name, Action<IServiceProvider, HttpClient> configureClient)
        //{

        //}
    }
}
