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
    public class NetStd20HttpResponse : WebResponse
    {
        protected const string FIELD_SET_COOKIE = "Set-Cookie";


        private HttpResponseMessage mResponse;
        private WebHeaderCollection mHeaders;
        private Dictionary<string, IEnumerable<string>> mHeaders2;
        private CookieCollection mCookies;


        public NetStd20HttpResponse(HttpResponseMessage responseMessage)
        {
            mResponse = responseMessage;
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


        /// <summary>
        /// Returns the cookies found Set-Cookie headers (RFC 2109) of server's response message.
        /// Does NOT support Set-Cookie2 headers (RFC 2965).
        /// </summary>
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
                                Cookie cookie = DecodeSetCookie(setCookieHeader);
                                mCookies.Add(cookie);
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
        public virtual Dictionary<string, IEnumerable<string>> Headers2
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


        public virtual HttpStatusCode StatusCode {
            get
            {
                VerifyDisposed();
                return mResponse.StatusCode;
            }
        }


        public virtual string StatusDescription
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
        /// Decodes the Set-Cookie header string (RFC 2109).
        /// Returns null if the string is not valid.
        /// </summary>
        protected static Cookie DecodeSetCookie(string setCookieHeaderString)
        {
            if (string.IsNullOrEmpty(setCookieHeaderString))
            {
                return null;
            }

            string[] keyValueArray = setCookieHeaderString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (keyValueArray == null || keyValueArray.Length == 0)
            {
                return null;
            }

            var result = new Cookie()
            {
                HttpOnly = false,
                Secure = false
            };
            bool isFirstLoop = true;

            foreach (string keyValuePair in keyValueArray)
            {
                string trimmedCookieItem = keyValuePair.Trim(' ');
                string[] kvPair = trimmedCookieItem.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                if (isFirstLoop)
                {
                    isFirstLoop = false;
                    // This must be the cookie NAME=VALUE pair, look to see if it is an existing cookie.
                    if (kvPair == null || kvPair.Length != 2)
                    {
                        return null; // Invalid Set-Cookie string.
                    }
                    result.Name = kvPair[0];
                    result.Value = kvPair[1];
                }
                else
                {
                    UpdateCookieAttributes(kvPair, result);
                }
            }

            return result;
        }


        private static void UpdateCookieAttributes(string[] kvPair, Cookie result)
        {
            if (kvPair != null && kvPair.Length == 2)
            {
                // This is an attribute=value pair.
                if ("Comment".Equals(kvPair[0], StringComparison.OrdinalIgnoreCase))
                {
                    result.Comment = kvPair[1];
                }
                else if ("Domain".Equals(kvPair[0], StringComparison.OrdinalIgnoreCase))
                {
                    result.Domain = kvPair[1];
                }
                else if ("Max-Age".Equals(kvPair[0], StringComparison.OrdinalIgnoreCase))
                {
                    int deltaSeconds;
                    if (int.TryParse(kvPair[1], out deltaSeconds))
                    {
                        if (deltaSeconds == 0)
                        {
                            // Expires immediately. Expired property is calculated from Expires DateTime.
                            result.Expires = DateTime.UtcNow;
                        }
                        else
                        {
                            result.Expires = result.TimeStamp + TimeSpan.FromSeconds(deltaSeconds);
                        }
                    }
                }
                else if ("Path".Equals(kvPair[0], StringComparison.OrdinalIgnoreCase))
                {
                    result.Path = kvPair[1];
                }
                else if ("Version".Equals(kvPair[0], StringComparison.OrdinalIgnoreCase))
                {
                    result.Version = 0;
                    int ver;
                    if (int.TryParse(kvPair[1], out ver))
                    {
                        result.Version = ver;
                    }
                }

            }
            else if (kvPair != null && kvPair.Length == 1)
            {
                // Attribute with just a key, e.g. HttpOnly
                if ("Secure".Equals(kvPair[0], StringComparison.OrdinalIgnoreCase))
                {
                    result.Secure = true;
                }
                else if ("HttpOnly".Equals(kvPair[0], StringComparison.OrdinalIgnoreCase))
                {
                    result.HttpOnly = true;
                }
            }
        }

    }
}
