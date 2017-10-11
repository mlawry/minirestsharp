#region License

//   Copyright 2010 John Sheehan
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 

#endregion

using MiniRestSharpCore.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniRestSharpCore.Http
{
    /// <summary>
    /// HttpWebRequest wrapper.
    /// This class is based on RestSharp's Http.cs file.
    /// </summary>
    public partial class NetCore11HttpEngine : IHttp
    {
        private const string LINE_BREAK = "\r\n";

        /// <summary>
        /// True if this HTTP request has any HTTP parameters
        /// </summary>
        protected bool HasParameters
        {
            get { return this.Parameters.Any(); }
        }

        /// <summary>
        /// True if this HTTP request has any HTTP cookies
        /// </summary>
        protected bool HasCookies
        {
            get { return this.Cookies.Any(); }
        }

        /// <summary>
        /// True if a request body has been specified
        /// </summary>
        protected bool HasBody
        {
            get { return this.RequestBodyBytes != null || !string.IsNullOrEmpty(this.RequestBody); }
        }

        /// <summary>
        /// True if files have been set to be uploaded
        /// </summary>
        protected bool HasFiles
        {
            get { return this.Files.Any(); }
        }

        /// <summary>
        /// Always send a multipart/form-data request - even when no Files are present.
        /// </summary>
        public bool AlwaysMultipartFormData { get; set; }

        /// <summary>
        /// UserAgent to be sent with request
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Timeout in milliseconds to be used for the request
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// System.Net.ICredentials to be sent with request
        /// </summary>
        public ICredentials Credentials { get; set; }

        /// <summary>
        /// The System.Net.CookieContainer to be used for the request
        /// </summary>
        public CookieContainer CookieContainer { get; set; }

        /// <summary>
        /// The method to use to write the response instead of reading into RawBytes
        /// </summary>
        public Action<Stream> ResponseWriter { get; set; }

        /// <summary>
        /// Collection of files to be sent with request
        /// </summary>
        public IList<HttpFile> Files { get; private set; }

        /// <summary>
        /// Whether or not HTTP 3xx response redirects should be automatically followed
        /// </summary>
        public bool FollowRedirects { get; set; }

        /// <summary>
        /// Maximum number of automatic redirects to follow if FollowRedirects is true
        /// </summary>
        public int? MaxRedirects { get; set; }

        /// <summary>
        /// Determine whether or not the "default credentials" (e.g. the user account under which the current process is running)
        /// will be sent along to the server.
        /// </summary>
        public bool UseDefaultCredentials { get; set; }

        public Encoding Encoding { get; set; }

        /// <summary>
        /// HTTP headers to be sent with request
        /// </summary>
        public IList<HttpHeader> Headers { get; private set; }

        /// <summary>
        /// HTTP parameters (QueryString or Form values) to be sent with request
        /// </summary>
        public IList<HttpParameter> Parameters { get; private set; }

        /// <summary>
        /// HTTP cookies to be sent with request
        /// </summary>
        public IList<HttpCookie> Cookies { get; private set; }

        /// <summary>
        /// Request body to be sent with request
        /// </summary>
        public string RequestBody { get; set; }

        /// <summary>
        /// Content type of the request body.
        /// </summary>
        public string RequestContentType { get; set; }

        /// <summary>
        /// An alternative to RequestBody, for when the caller already has the byte array.
        /// </summary>
        public byte[] RequestBodyBytes { get; set; }

        /// <summary>
        /// URL to call for this request
        /// </summary>
        public Uri Url { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public NetCore11HttpEngine()
        {
            this.Headers = new List<HttpHeader>();
            this.Files = new List<HttpFile>();
            this.Parameters = new List<HttpParameter>();
            this.Cookies = new List<HttpCookie>();
            this.restrictedHeaderActions = new Dictionary<string, Action<HttpRequestMessage, string>>(
                StringComparer.OrdinalIgnoreCase);

            this.AddSharedHeaderActions();
            this.AddSyncHeaderActions();
        }

        partial void AddSyncHeaderActions();

        private void AddSharedHeaderActions()
        {
            this.restrictedHeaderActions.Add("Date", (r, v) => { /* Set by system */ });
            this.restrictedHeaderActions.Add("Host", (r, v) => { /* Set by system */ });
            this.restrictedHeaderActions.Add("Range", AddRange);
        }

        private const string FORM_BOUNDARY = "-----------------------------28947758029299";

        private static string GetMultipartFormContentType()
        {
            return string.Format("multipart/form-data; boundary={0}", FORM_BOUNDARY);
        }

        private static string GetMultipartFileHeader(HttpFile file)
        {
            return string.Format(
                "--{0}{4}Content-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"{4}Content-Type: {3}{4}{4}",
                FORM_BOUNDARY, file.Name, file.FileName, file.ContentType ?? "application/octet-stream", LINE_BREAK);
        }

        private string GetMultipartFormData(HttpParameter param)
        {
            string format = param.Name == this.RequestContentType
                ? "--{0}{3}Content-Type: {4}{3}Content-Disposition: form-data; name=\"{1}\"{3}{3}{2}{3}"
                : "--{0}{3}Content-Disposition: form-data; name=\"{1}\"{3}{3}{2}{3}";

            return string.Format(format, FORM_BOUNDARY, param.Name, param.Value, LINE_BREAK, param.ContentType);
        }

        private static string GetMultipartFooter()
        {
            return string.Format("--{0}--{1}", FORM_BOUNDARY, LINE_BREAK);
        }

        private readonly IDictionary<string, Action<HttpRequestMessage, string>> restrictedHeaderActions;

        // handle restricted headers the .NET way - thanks @dimebrain!
        // http://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.headers.aspx
        private void AppendHeaders(HttpRequestMessage request)
        {
            foreach (HttpHeader header in this.Headers)
            {
                if (this.restrictedHeaderActions.ContainsKey(header.Name))
                {
                    this.restrictedHeaderActions[header.Name].Invoke(request, header.Value);
                }
                else
                {
                    // HttpRequestMessage requires some well-known headers to appear in the Content property rather than the Headers property.
                    if (IsHttpContentHeader(header.Name))
                    {
                        request.Content.Headers.TryAddWithoutValidation(header.Name, header.Value);
                    }
                    else
                    {
                        request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                    }
                }
            }
        }

        private static bool IsHttpContentHeader(string headerName)
        {
            if (headerName == null)
            {
                throw new ArgumentNullException(nameof(headerName));
            }
            else if (
                headerName.Equals("Allow", StringComparison.OrdinalIgnoreCase) ||
                headerName.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase) ||
                headerName.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase) ||
                headerName.Equals("Content-Language", StringComparison.OrdinalIgnoreCase) ||
                headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                headerName.Equals("Content-Location", StringComparison.OrdinalIgnoreCase) ||
                headerName.Equals("Content-MD5", StringComparison.OrdinalIgnoreCase) ||
                headerName.Equals("Content-Range", StringComparison.OrdinalIgnoreCase) ||
                headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                headerName.Equals("Expires", StringComparison.OrdinalIgnoreCase) ||
                headerName.Equals("Last-Modified", StringComparison.OrdinalIgnoreCase)
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void AppendCookies(HttpClientHandler handler, Uri requestUri)
        {
            handler.CookieContainer = this.CookieContainer ?? new CookieContainer();

            foreach (HttpCookie httpCookie in this.Cookies)
            {
                Cookie cookie = new Cookie
                                {
                                    Name = httpCookie.Name,
                                    Value = httpCookie.Value,
                                    Domain = requestUri.Host
                                };

                handler.CookieContainer.Add(new Uri(string.Format("{0}://{1}", requestUri.Scheme, requestUri.Host)), cookie);
            }

            if (handler.CookieContainer == null || handler.CookieContainer.Count == 0)
            {
                //handler.CookieContainer = null; // In netstandard2.0 this property cannot be set to null.
                handler.UseCookies = false;
            }
            else
            {
                handler.UseCookies = true;
            }
        }

        private string EncodeParameters()
        {
            StringBuilder querystring = new StringBuilder();

            foreach (HttpParameter p in this.Parameters)
            {
                if (querystring.Length > 1)
                {
                    querystring.Append("&");
                }

                querystring.AppendFormat("{0}={1}", p.Name.UrlEncode(), p.Value.UrlEncode());
            }

            return querystring.ToString();
        }

        private void PreparePostBody(HttpRequestMessage request)
        {
            if (this.HasFiles || this.AlwaysMultipartFormData)
            {
                string contentTypeStr = GetMultipartFormContentType();
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentTypeStr);
            }
            else if (this.HasParameters)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                this.RequestBody = this.EncodeParameters();
            }
            else if (this.HasBody)
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(this.RequestContentType);
            }
        }

        private void WriteStringTo(Stream stream, string toWrite)
        {
            byte[] bytes = this.Encoding.GetBytes(toWrite);

            stream.Write(bytes, 0, bytes.Length);
        }

        private void WriteMultipartFormData(Stream requestStream)
        {
            foreach (HttpParameter param in this.Parameters)
            {
                this.WriteStringTo(requestStream, this.GetMultipartFormData(param));
            }

            foreach (HttpFile file in this.Files)
            {
                // Add just the first part of this param, since we will write the file data directly to the Stream
                this.WriteStringTo(requestStream, GetMultipartFileHeader(file));

                // Write the file data directly to the Stream, rather than serializing it to a string.
                file.Writer(requestStream);
                this.WriteStringTo(requestStream, LINE_BREAK);
            }

            this.WriteStringTo(requestStream, GetMultipartFooter());
        }

        private void ExtractResponseData(HttpResponse response, NetCore11HttpResponse webResponse)
        {
            using (webResponse)
            {

                response.ContentEncoding = webResponse.Headers[HttpResponseHeader.ContentEncoding];
                response.Server = webResponse.Headers[HttpResponseHeader.Server];

                response.ContentType = webResponse.ContentType;
                response.ContentLength = webResponse.ContentLength;

                Stream webResponseStream = webResponse.GetResponseStream();


                if (string.Equals(webResponse.Headers[HttpResponseHeader.ContentEncoding], "gzip", StringComparison.OrdinalIgnoreCase))
                {
                    GZipStream gzStream = new GZipStream(webResponseStream, CompressionMode.Decompress);

                    ProcessResponseStream(gzStream, response);
                }
                else
                {
                    ProcessResponseStream(webResponseStream, response);
                }

                response.StatusCode = webResponse.StatusCode;
                response.StatusDescription = webResponse.StatusDescription;
                response.ResponseUri = webResponse.ResponseUri;
                response.ResponseStatus = ResponseStatus.Completed;

                if (webResponse.Cookies != null)
                {
                    foreach (Cookie cookie in webResponse.Cookies)
                    {
                        response.Cookies.Add(new HttpCookie
                        {
                            Comment = cookie.Comment,
                            CommentUri = cookie.CommentUri,
                            Discard = cookie.Discard,
                            Domain = cookie.Domain,
                            Expired = cookie.Expired,
                            Expires = cookie.Expires,
                            HttpOnly = cookie.HttpOnly,
                            Name = cookie.Name,
                            Path = cookie.Path,
                            Port = cookie.Port,
                            Secure = cookie.Secure,
                            TimeStamp = cookie.TimeStamp,
                            Value = cookie.Value,
                            Version = cookie.Version
                        });
                    }
                }

                foreach (string headerName in webResponse.Headers.AllKeys)
                {
                    string headerValue = webResponse.Headers[headerName];

                    response.Headers.Add(new HttpHeader
                    {
                        Name = headerName,
                        Value = headerValue
                    });
                }

                webResponse.Dispose();
            }
        }

        private void ProcessResponseStream(Stream webResponseStream, HttpResponse response)
        {
            if (this.ResponseWriter == null)
            {
                response.RawBytes = webResponseStream.ReadAsBytes();
            }
            else
            {
                this.ResponseWriter(webResponseStream);
            }
        }


        private static void AddRange(HttpRequestMessage r, string range)
        {
            Match m = Regex.Match(range, "(\\w+)=(\\d+)-(\\d+)$");

            if (!m.Success)
            {
                return;
            }

            string rangeSpecifier = m.Groups[1].Value;
            int from = Convert.ToInt32(m.Groups[2].Value);
            int to = Convert.ToInt32(m.Groups[3].Value);

            if (!"bytes".Equals(rangeSpecifier, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Only the 'bytes' range specifier is supported.");
            }

            r.Headers.Range = new RangeHeaderValue(from, to);
        }

    }
}
