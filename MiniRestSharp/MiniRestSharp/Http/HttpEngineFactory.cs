using System;
using System.Collections.Concurrent;
using System.Net.Http;

namespace MiniRestSharpCore.Http
{
    /// <summary>
    /// <para>
    /// To promote the re-use of <see cref="HttpClientHandler"/> instances, this method now supported
    /// named clients (and may be typed clients later on).
    /// </para>
    /// <para>
    /// See the following articles for evidence that we should re-use HttpClientHandlers when creating HttpClients
    /// https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
    /// https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
    /// https://stackoverflow.com/questions/15705092/do-httpclient-and-httpclienthandler-have-to-be-disposed
    /// </para>
    /// <para>
    /// The life-time of this factory class must now be a singleton.
    /// </para>
    /// </summary>
    internal class HttpEngineFactory : IHttpFactory
    {
        /// <summary>
        /// We use NamedHttpClientHandlerWrapper instances to store HttpClientHandlers associated with the same name,
        /// one instance per unique name. Not exactly sure whether IHttpClientFactory named clients are case
        /// sensitive or not. But making them case sensitive is safer than assuming they are insensitive.
        /// </summary>
        private ConcurrentDictionary<string, NamedHttpClientHandlerWrapper> mNamedHandlerWrappers = new ConcurrentDictionary<string, NamedHttpClientHandlerWrapper>(StringComparer.Ordinal);


        public IHttp Create(string namedClientName)
        {
            if (namedClientName == null)
            {
                // This is the old behaviour. NetStd20HttpEngine will use new HttpClient and new HttpClientHandler when
                // creating NetStd20HttpRequests, see NetStd20HttpEngine.CreateWebRequest(string method, Uri url).
                return new NetStd20HttpEngine();
            }
            else
            {
                // The new behaviour is to create a new NetStd20HttpEngine instance but link it to a NamedHttpClientHandlerWrapper
                // (the behaviour implemented by the subclass NamedClientHttpEngine). This way we can easily ensure that
                // all HttpClientHandlers for the same named client are initialised just once (because they are re-used from
                // within a pool, while HttpClients are created new for each request, and HttpClientHandler properties cannot
                // change once used.)
                //
                // Note however, that we don't know which named HttpClient is mapped to which HttpClientHandler, all we know
                // is that the HttpClient is newly created, and it is linked with one of the HttpClientHandlers in
                // NamedHttpClientHandlerWrapper (because they have the same name). And we know the HttpClientHandlers in
                // NamedHttpClientHandlerWrapper hasn't been configured yet (because once configured, they are removed
                // from the wrapper).
                //
                // Such a set up can ensure all HttpClientHandlers with the same name are configured the same way, so then
                // no matter whichever HttpClientHandler the HttpClient is paired with, it will work in an expected way.
                //
                // However, there is an assumption that the user of MiniRestSharp does not attempt to use the same named client
                // with different set up (specifically, set ups that require different HttpClientHandler configurations).

                // This call is thread-safe because mNamedHandlerWrappers is a ConcurrentDictionary.
                NamedHttpClientHandlerWrapper wrapper = mNamedHandlerWrappers.GetOrAdd(namedClientName, name => new NamedHttpClientHandlerWrapper(name));
                return new NamedClientHttpEngine(namedClientName, wrapper);
            }
        }


        /// <summary>
        /// This method will wrap the HttpClientHandler given in a NamedHttpClientHandlerWrapper for the given name.
        /// A single NamedHttpClientHandlerWrapper can wrap many HttpClientHandlers, but all for the same name.
        /// This method is thread-safe.
        /// </summary>
        public void AddNamedClientHandler(string name, HttpClientHandler httpClientHandler)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (httpClientHandler == null)
            {
                throw new ArgumentNullException(nameof(httpClientHandler));
            }

            // Since we cannot guarantee this method won't be called concurrently (e.g. multiple requests for the same named
            // client), we must make sure it is thread-safe. Luckily mNamedHandlerWrappers is a ConcurrentDictionary.
            NamedHttpClientHandlerWrapper wrapper = mNamedHandlerWrappers.GetOrAdd(name, nam => new NamedHttpClientHandlerWrapper(nam));

            // Add the handler to the wrapper for the given name. Wrapper object is NOT thread-safe so we must
            // synchronize on it before adding. An implication is that this may block if the same wrapper object is being
            // used (as NetStd20HttpRequest.RequestHandler) in the NamedClientHttpEngine.ConfigureWebRequest() method below.
            lock (wrapper)
            {
                wrapper.Add(httpClientHandler);
            }
        }


        /// <summary>
        /// Extends NetStd20HttpEngine to provide support for named clients and client handlers.
        /// </summary>
        private class NamedClientHttpEngine : NetStd20HttpEngine
        {
            private readonly string mName;
            private readonly NamedHttpClientHandlerWrapper mHttpClientHandlerWrapper;
            private HttpClient mHttpClient;


            internal NamedClientHttpEngine(string name, NamedHttpClientHandlerWrapper httpClientHandlerWrapper)
            {
                mName = name;
                mHttpClientHandlerWrapper = httpClientHandlerWrapper;
            }


            /// <summary>
            /// Retains the given named client, using it to create <see cref="NetStd20HttpRequest"/> instances later
            /// in the <see cref="CreateWebRequest(string, Uri)"/> method.
            /// </summary>
            public override void SetNamedHttpClient(string name, HttpClient httpClient)
            {
                if (httpClient == null)
                {
                    throw new ArgumentNullException(nameof(httpClient));
                }
                if (!mName.Equals(name, StringComparison.Ordinal))
                {
                    throw new ArgumentException(string.Format("This engine instance does not manage clients of the given name. Expected: {0}. Given: {1}.", mName, name), nameof(name));
                }

                mHttpClient = httpClient;
            }


            /// <summary>
            /// Returns a new request using the named client and handlers.
            /// </summary>
            protected override NetStd20HttpRequest CreateWebRequest(string method, Uri url)
            {
                // Sanity check to make sure we have been given the named client and handlers already.
                if (mHttpClient == null || mHttpClientHandlerWrapper == null)
                {
                    throw new InvalidOperationException("Cannot create NetStd20HttpRequest because named HttpClient and HttpClientHandlers haven't been provided yet.");
                }

                return new NetStd20HttpRequest(mHttpClient, mHttpClientHandlerWrapper, method, url);
            }


            /// <summary>
            /// This is the method that is called to configure the <see cref="NetStd20HttpRequest.RequestClient"/>
            /// and <see cref="NetStd20HttpRequest.RequestHandler"/> returned by <see cref="CreateWebRequest(string, Uri)"/>.
            /// </summary>
            /// <param name="request">Value returned by <see cref="CreateWebRequest(string, Uri)"/>.</param>
            protected override NetStd20HttpRequest ConfigureWebRequest(NetStd20HttpRequest request)
            {
                // Must ensure this method is thread-safe because the parent method will update
                // NetStd20HttpRequest.RequestHandler properties, which is really just calling NamedHttpClientHandlerWrapper.
                // We don't want multiple threads to be updating NamedHttpClientHandlerWrapper properties at the same
                // time because the wrapped HttpClientHandler properties are probably not thread-safe, and we don't want
                // another thread to add to the wrapper while we are updating wrapper's current set of handlers.
                NamedHttpClientHandlerWrapper wrapper = request.RequestHandler;

                // The lock will prevent new handlers from being added in the HttpEngineFactory.AddNamedClientHandler() method above.
                lock (wrapper)
                {
                    // By this point, it is an error if we do not have access to the HttpClientHandler associated with the named client.
                    // Because we clear the wrapper at the end of this code block, wrapper may be empty if we had previously
                    // configured the named HttpClientHandler already. The IsAlwaysEmpty property gets around this issue.
                    if (wrapper.IsAlwaysEmpty)
                    {
                        throw new InvalidOperationException(string.Format(
                            "HttpClientHandler for the named client '{0}' has not been registered with MiniRestSharp. Did you forget to call RestSharpServiceCollectionExtensions.AddRestSharpClient(IServiceCollection, string)?",
                            wrapper.Name));
                    }

                    // I've confirmed that the only place the wrapped HttpClientHandlers are configured is in this method.
                    NetStd20HttpRequest returnValue = base.ConfigureWebRequest(request);

                    // Now that the wrapped HttpClientHandlers are configured, we remove them from the wrapper
                    // since we can't configure them again anyway. It's of course possible that these handlers
                    // get recycled by IHttpClientFactory and become configurable again. When this happens I think the
                    // RestSharpServiceCollectionExtensions system will end up calling HttpEngineFactory.AddNamedClientHandler()
                    // again, which will result in it being added to wrapper again for another round of configuration later.
                    wrapper.Clear();

                    return returnValue;
                }
            }
        }

    }
}
