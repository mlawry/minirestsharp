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
        /// <summary>
        /// This constructor will initialise <see cref="Handlers"/> as a new empty list.
        /// </summary>
        public NamedHttpClientHandlerWrapper()
        {
            this.Handlers = new List<HttpClientHandler>();
        }

        public List<HttpClientHandler> Handlers { get; }

        public DecompressionMethods AutomaticDecompression
        {
            set
            {
                foreach (HttpClientHandler h in this.Handlers)
                {
                    h.AutomaticDecompression = value;
                }
            }
        }

        public bool UseDefaultCredentials
        {
            set
            {
                foreach (HttpClientHandler h in this.Handlers)
                {
                    h.UseDefaultCredentials = value;
                }
            }
        }

        public ICredentials Credentials
        {
            set
            {
                foreach (HttpClientHandler h in this.Handlers)
                {
                    h.Credentials = value;
                }
            }
        }

        public bool AllowAutoRedirect
        {
            set
            {
                foreach (HttpClientHandler h in this.Handlers)
                {
                    h.AllowAutoRedirect = value;
                }
            }
        }

        public int MaxAutomaticRedirections
        {
            set
            {
                foreach (HttpClientHandler h in this.Handlers)
                {
                    h.MaxAutomaticRedirections = value;
                }
            }
        }

        public bool UseCookies
        {
            set
            {
                foreach (HttpClientHandler h in this.Handlers)
                {
                    h.UseCookies = value;
                }
            }
        }

        public CookieContainer CookieContainer
        {
            set
            {
                foreach (HttpClientHandler h in this.Handlers)
                {
                    h.CookieContainer = value;
                }
            }
        }

        public void CookieContainer_Add(Uri cookieUri, Func<Cookie> createCookie)
        {
            foreach (HttpClientHandler h in this.Handlers)
            {
                Cookie cookie = createCookie.Invoke();
                h.CookieContainer.Add(cookieUri, cookie);
            }
        }

    }
}
