using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace MiniRestSharpCore.Http
{
    /// <summary>
    /// Equivalent to HttpWebResponse, based on that classes' code.
    /// </summary>
    internal class NetCore11HttpResponse : WebResponse
    {
        private HttpResponseMessage mResponse;
        private Dictionary<string, IEnumerable<string>> mHeaders;


        internal NetCore11HttpResponse(HttpResponseMessage responseMessage)
        {
            this.mResponse = responseMessage;
        }


        public override long ContentLength
        {
            get
            {
                CheckDisposed();
                long? length = mResponse?.Content?.Headers?.ContentLength;
                return length ?? -1;
            }
        }


        public override string ContentType
        {
            get
            {
                CheckDisposed();

                MediaTypeHeaderValue typeValue = mResponse?.Content?.Headers?.ContentType;
                if (typeValue == null)
                {
                    return "";
                }

                return typeValue.ToString();
            }
        }


        public virtual CookieCollection Cookies
        {
            get
            {
                CheckDisposed();
                return _cookies;
            }
        }


        public Dictionary<string, IEnumerable<string>> Headers2
        {
            get
            {
                CheckDisposed();
                if (mHeaders == null)
                {
                    mHeaders = new Dictionary<string, IEnumerable<string>>();
                    if (mResponse?.Headers != null)
                    {
                        foreach (KeyValuePair<string, IEnumerable<string>> header in mResponse.Headers)
                        {
                            mHeaders[header.Key] = header.Value;
                        }

                        if (mResponse?.Content?.Headers != null)
                        {
                            foreach (KeyValuePair<string, IEnumerable<string>> header in mResponse.Content.Headers)
                            {
                                mHeaders[header.Key] = header.Value;
                            }
                        }
                    }
                }
                return mHeaders;
            }
        }


        public override Uri ResponseUri
        {
            get
            {
                CheckDisposed();

                // The underlying System.Net.Http API will automatically update
                // the .RequestUri property to be the final URI of the response.
                return mResponse.RequestMessage.RequestUri;
            }
        }


        public HttpStatusCode StatusCode {
            get
            {
                CheckDisposed();
                return mResponse.StatusCode;
            }
        }


        public string StatusDescription
        {
            get
            {
                CheckDisposed();
                return mResponse.ReasonPhrase;
            }
        }


        public override bool SupportsHeaders
        {
            get
            {
                return true;
            }
        }


        public override Stream GetResponseStream()
        {
            CheckDisposed();
            return mResponse.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        }


        protected override void Dispose(bool disposing)
        {
            var httpResponseMessage = mResponse;
            if (httpResponseMessage != null)
            {
                httpResponseMessage.Dispose();
                mResponse = null;
            }
        }


        private void CheckDisposed()
        {
            if (mResponse == null)
            {
                throw new ObjectDisposedException(this.GetType().ToString());
            }
        }

    }
}
