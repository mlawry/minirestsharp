using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace MiniRestSharpCore.Http
{
    /// <summary>
    /// Class designed where each object wraps multiple HttpClientHandlers for the same named client name.
    /// Assumes client names are case sensitive. This class is NOT thread-safe.
    /// </summary>
    public class NamedHttpClientHandlerWrapper
    {
        private readonly List<HttpClientHandler> mHandlerList;


        /// <summary>
        /// This constructor will initialise <see cref="IsAlwaysEmpty"/> to true.
        /// </summary>
        /// <param name="name">The <see cref="Name"/> property will be set to this value. May be null.</param>
        public NamedHttpClientHandlerWrapper(string name)
        {
            mHandlerList = new List<HttpClientHandler>();
            this.IsAlwaysEmpty = true;
            this.Name = name;
        }


        /// <summary>
        /// The named client name of the HttpClientHandlers added to this wrapper. This property is
        /// used for error reporting purposes only. May be null.
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// <para>
        /// Initially true to indicate that this wrapper has never been used to hold any client handler.
        /// Once a client handler has been added via <see cref="Add"/>, this property will change to false and
        /// remain false forever.
        /// </para>
        /// <para>
        /// This property is used to detect if
        /// <see cref="Microsoft.Extensions.DependencyInjection.RestSharpServiceCollectionExtensions.AddRestSharpClient(Microsoft.Extensions.DependencyInjection.IServiceCollection, string)"/>
        /// had been called for the named client used when calling
        /// <see cref="IRestRequest.UseNamedHttpClient(IHttpClientFactory, string)"/>.
        /// </para>
        /// </summary>
        public bool IsAlwaysEmpty { get; private set; }


        /// <summary>
        /// Add a client handler to this wrapper. This method also sets <see cref="IsAlwaysEmpty"/> to false.
        /// </summary>
        public void Add(HttpClientHandler clientHandler)
        {
            this.IsAlwaysEmpty = false;
            mHandlerList.Add(clientHandler);
        }


        /// <summary>
        /// Removes all previously added client handlers. This method does not change <see cref="IsAlwaysEmpty"/>.
        /// </summary>
        public void Clear()
        {
            mHandlerList.Clear();
        }



        public DecompressionMethods AutomaticDecompression
        {
            set
            {
                foreach (HttpClientHandler h in mHandlerList)
                {
                    h.AutomaticDecompression = value;
                }
            }
        }

        public bool UseDefaultCredentials
        {
            set
            {
                foreach (HttpClientHandler h in mHandlerList)
                {
                    h.UseDefaultCredentials = value;
                }
            }
        }

        public ICredentials Credentials
        {
            set
            {
                foreach (HttpClientHandler h in mHandlerList)
                {
                    h.Credentials = value;
                }
            }
        }

        public bool AllowAutoRedirect
        {
            set
            {
                foreach (HttpClientHandler h in mHandlerList)
                {
                    h.AllowAutoRedirect = value;
                }
            }
        }

        public int MaxAutomaticRedirections
        {
            set
            {
                foreach (HttpClientHandler h in mHandlerList)
                {
                    h.MaxAutomaticRedirections = value;
                }
            }
        }

        public bool UseCookies
        {
            set
            {
                foreach (HttpClientHandler h in mHandlerList)
                {
                    h.UseCookies = value;
                }
            }
        }

        public CookieContainer CookieContainer
        {
            set
            {
                foreach (HttpClientHandler h in mHandlerList)
                {
                    h.CookieContainer = value;
                }
            }
        }

        public void CookieContainer_Add(Uri cookieUri, Func<Cookie> createCookie)
        {
            foreach (HttpClientHandler h in mHandlerList)
            {
                Cookie cookie = createCookie.Invoke();
                h.CookieContainer.Add(cookieUri, cookie);
            }
        }

    }
}
