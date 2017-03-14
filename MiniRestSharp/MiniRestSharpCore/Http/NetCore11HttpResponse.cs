using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MiniRestSharpCore.Http
{
    /// <summary>
    /// Equivalent to HttpWebResponse.
    /// </summary>
    internal class NetCore11HttpResponse : WebResponse
    {
        private const string FIELD_SET_COOKIE = "Set-Cookie";


        private HttpResponseMessage mResponse;
        private WebHeaderCollection mHeaders;
        private Dictionary<string, IEnumerable<string>> mHeaders2;
        private CookieCollection mCookies;


        internal NetCore11HttpResponse(HttpResponseMessage responseMessage)
        {
            this.mResponse = responseMessage;
        }


        public override long ContentLength
        {
            get
            {
                VerifyDisposed();
                long? length = mResponse?.Content?.Headers?.ContentLength;
                return length ?? -1;
            }
        }


        public override string ContentType
        {
            get
            {
                VerifyDisposed();

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
                VerifyDisposed();
                if (mCookies == null)
                {
                    mCookies = new CookieCollection();
                    if (mResponse?.Headers != null)
                    {
                        IEnumerable<string> setCookieValues;
                        if (mResponse.Headers.TryGetValues(FIELD_SET_COOKIE, out setCookieValues))
                        {
                            foreach (string setCookieHeader in setCookieValues)
                            {
                                Dictionary<string, string> cookieDict = DecodeSetCookie(setCookieHeader);

                                var cookie = new Cookie();
                                cookie.
                            }
                        }
                    }
                }
                return mCookies;
            }
        }


        /// <summary>
        /// Returns only the first value for each unique header name.
        /// </summary>
        public override WebHeaderCollection Headers
        {
            get
            {
                VerifyDisposed();
                if (mHeaders == null)
                {
                    mHeaders = new WebHeaderCollection();
                    if (mResponse?.Headers != null)
                    {
                        foreach (KeyValuePair<string, IEnumerable<string>> header in mResponse.Headers)
                        {
                            mHeaders[header.Key] = header.Value.First();
                        }

                        if (mResponse?.Content?.Headers != null)
                        {
                            foreach (KeyValuePair<string, IEnumerable<string>> header in mResponse.Content.Headers)
                            {
                                mHeaders[header.Key] = header.Value.First();
                            }
                        }
                    }
                }
                return mHeaders;
            }
        }


        /// <summary>
        /// Returns all header values.
        /// </summary>
        public Dictionary<string, IEnumerable<string>> Headers2
        {
            get
            {
                VerifyDisposed();
                if (mHeaders2 == null)
                {
                    mHeaders2 = new Dictionary<string, IEnumerable<string>>();
                    if (mResponse?.Headers != null)
                    {
                        foreach (KeyValuePair<string, IEnumerable<string>> header in mResponse.Headers)
                        {
                            mHeaders2[header.Key] = header.Value;
                        }

                        if (mResponse?.Content?.Headers != null)
                        {
                            foreach (KeyValuePair<string, IEnumerable<string>> header in mResponse.Content.Headers)
                            {
                                mHeaders2[header.Key] = header.Value;
                            }
                        }
                    }
                }
                return mHeaders2;
            }
        }


        public override Uri ResponseUri
        {
            get
            {
                VerifyDisposed();
                return mResponse.RequestMessage.RequestUri;
            }
        }


        public HttpStatusCode StatusCode {
            get
            {
                VerifyDisposed();
                return mResponse.StatusCode;
            }
        }


        public string StatusDescription
        {
            get
            {
                VerifyDisposed();
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
            VerifyDisposed();
            return mResponse.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        }


        protected override void Dispose(bool disposing)
        {
            var response = mResponse;
            if (response != null)
            {
                response.Dispose();
                mResponse = null;
            }
        }


        private void VerifyDisposed()
        {
            if (mResponse == null)
            {
                throw new ObjectDisposedException("This object has been disposed.");
            }
        }


        /// <summary>
        /// Decodes the Set-Cookie header string in a HTTP response into key value pairs, where the key is attribute name
        /// and value is attribute value. If cookieHeaderString is null or invalid, returns empty dictionary.
        /// If attribute does not have a value (e.g. HttpOnly), then an empty string will be used as the value.
        /// Does not return null.
        /// </summary>
        private static List<KeyValuePair<string, string>> DecodeSetCookie(string setCookieHeaderString)
        {
            var result = new List<KeyValuePair<string, string>>();

            if (string.IsNullOrEmpty(setCookieHeaderString))
            {
                return result;
            }

            string[] keyValueArray = setCookieHeaderString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (keyValueArray == null || keyValueArray.Length == 0)
            {
                return result;
            }

            foreach (string keyValuePair in keyValueArray)
            {
                string trimmedCookieItem = keyValuePair.Trim(' ');
                string[] kvPair = trimmedCookieItem.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (kvPair != null && kvPair.Length == 2)
                {
                    result[kvPair[0]] = kvPair[1];
                }
                else if (kvPair != null && kvPair.Length == 1)
                {
                    // Attribute with just a key, e.g. HttpOnly
                    result[kvPair[0]] = "";
                }
            }

            return result;
        }

    }
}
