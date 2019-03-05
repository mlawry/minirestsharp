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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MiniRestSharpCore.Http
{
    /// <summary>
    /// HttpWebRequest wrapper (sync methods)
    /// This class is based on RestSharp's Http.Sync.cs file.
    /// </summary>
    public partial class NetStd20HttpEngine
    {
        /// <summary>
        /// Execute a GET-style request with the specified HTTP Method.  
        /// </summary>
        /// <param name="httpMethod">The HTTP method to execute.</param>
        /// <returns></returns>
        public async Task<HttpResponse> AsGetTaskAsync(string httpMethod)
        {
            return await this.GetStyleMethodInternal(httpMethod.ToUpperInvariant());
        }

        /// <summary>
        /// Execute a POST-style request with the specified HTTP Method.  
        /// </summary>
        /// <param name="httpMethod">The HTTP method to execute.</param>
        /// <returns></returns>
        public async Task<HttpResponse> AsPostTaskAsync(string httpMethod)
        {
            return await this.PostPutInternal(httpMethod.ToUpperInvariant());
        }

        /// <summary>
        /// Override this method to provide your own implementation of creating a new NetStd20HttpRequest instance.
        /// </summary>
        protected virtual NetStd20HttpRequest CreateWebRequest(string method, Uri url)
        {
            return new NetStd20HttpRequest(method, url);
        }

        private async Task<HttpResponse> GetStyleMethodInternal(string method)
        {
            NetStd20HttpRequest request = this.CreateWebRequest(method, this.Url);
            this.ConfigureWebRequest(request);

            if (this.HasBody && (method == "DELETE" || method == "OPTIONS"))
            {
                request.RequestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(this.RequestContentType);
                this.WriteRequestBody(request);
            }

            return await this.GetResponse(request);
        }

        private async Task<HttpResponse> PostPutInternal(string method)
        {
            NetStd20HttpRequest request = this.CreateWebRequest(method, this.Url);
            this.ConfigureWebRequest(request);

            this.PreparePostBody(request.RequestMessage);

            this.WriteRequestBody(request);
            return await this.GetResponse(request);
        }

        partial void AddSyncHeaderActions()
        {
            this.restrictedHeaderActions.Add("Connection", (r, v) => {
                if (v.ToLowerInvariant().Contains("keep-alive"))
                {
                    //if a user sets the connection header explicitly to "keep-alive" then we set the field on HttpWebRequest
                    r.Headers.Connection.Add("keep-alive");
                }
                else
                {
                    //if "Connection" is specified as anything else, we turn off keep alive functions
                    r.Headers.Connection.Clear();
                }
            });
            // Implicit conversion from DateTime to DateTimeOffset
            this.restrictedHeaderActions.Add("If-Modified-Since", (r, v) => r.Headers.IfModifiedSince = Convert.ToDateTime(v));
        }

        private void ExtractErrorResponse(HttpResponse httpResponse, Exception ex)
        {
            WebException webException = ex as WebException;

            if (webException != null && webException.Status == WebExceptionStatus.Timeout)
            {
                httpResponse.ResponseStatus = ResponseStatus.TimedOut;
                httpResponse.ErrorMessage = ex.Message;
                httpResponse.ErrorException = webException;

                return;
            }

            httpResponse.ErrorMessage = ex.Message;
            httpResponse.ErrorException = ex;
            httpResponse.ResponseStatus = ResponseStatus.Error;
        }

        private async Task<HttpResponse> GetResponse(NetStd20HttpRequest request)
        {
            HttpResponse response = new HttpResponse { ResponseStatus = ResponseStatus.None };

            try
            {
                NetStd20HttpResponse webResponse = await GetRawResponse(request);

                this.ExtractResponseData(response, webResponse);
            }
            catch (Exception ex)
            {
                this.ExtractErrorResponse(response, ex);
            }

            return response;
        }

        private static async Task<NetStd20HttpResponse> GetRawResponse(NetStd20HttpRequest request)
        {
            try
            {
                // Some methods do not allow entity-body in request, so we must remove the HttpContent.
                // See https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods for which method allows/disallows entity-body.
                // We need to do this because in the constructor of NetStd20HttpRequest, we always set a HttpContent to make
                // programming easier. In netstandard2.0, the framework does not allow HttpContent for methods such as GET.
                HttpMethod requestMethod = request.RequestMessage.Method;
                if (requestMethod == HttpMethod.Delete ||
                    requestMethod == HttpMethod.Get ||
                    requestMethod == HttpMethod.Head ||
                    requestMethod == HttpMethod.Options)
                {
                    // Will also remove any HttpContent-specific headers that may have been set erroneously.
                    request.RequestMessage.Content = null;
                }

                return await request.GetResponseAsync();
            }
            catch (WebException ex)
            {
                // Check to see if this is an HTTP error or a transport error.
                // In cases where an HTTP error occurs ( status code >= 400 )
                // return the underlying HTTP response, otherwise assume a
                // transport exception (ex: connection timeout) and
                // rethrow the exception

                var exResponse = ex.Response as NetStd20HttpResponse;
                if (exResponse != null)
                {
                    return exResponse;
                }

                throw;
            }
        }

        private void WriteRequestBody(NetStd20HttpRequest webRequest)
        {
            if (this.HasBody || this.HasFiles || this.AlwaysMultipartFormData)
            {
                webRequest.RequestMessage.Content.Headers.ContentLength = this.CalculateContentLength();
            }

            using (Stream requestStream = webRequest.GetRequestStreamAsync())
            {
                if (this.HasFiles || this.AlwaysMultipartFormData)
                {
                    this.WriteMultipartFormData(requestStream);
                }
                else if (this.RequestBodyBytes != null)
                {
                    requestStream.Write(this.RequestBodyBytes, 0, this.RequestBodyBytes.Length);
                }
                else if (this.RequestBody != null)
                {
                    this.WriteStringTo(requestStream, this.RequestBody);
                }
            }
        }

        private NetStd20HttpRequest ConfigureWebRequest(NetStd20HttpRequest request)
        {
            ////////// 1. Configure HttpRequestMessage.
            this.AppendHeaders(request.RequestMessage);

            // Override certain headers that are controlled by RestSharp.
            request.RequestMessage.Headers.TE.Clear();
            request.RequestMessage.Headers.TE.Add(new TransferCodingWithQualityHeaderValue("deflate"));
            request.RequestMessage.Headers.TE.Add(new TransferCodingWithQualityHeaderValue("gzip"));
            request.RequestMessage.Headers.TE.Add(new TransferCodingWithQualityHeaderValue("identity"));

            if (this.UserAgent.HasValue())
            {
                request.RequestMessage.Headers.UserAgent.Add(ProductInfoHeaderValue.Parse(this.UserAgent));
            }

            ////////// 2. Configure HttpClientHandler
            this.AppendCookies(request.RequestHandler, request.RequestMessage.RequestUri);

            // This matches with request.Headers.TE header.
            request.RequestHandler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None;

            // Handler credentials.
            request.RequestHandler.UseDefaultCredentials = this.UseDefaultCredentials;
            if (this.Credentials != null)
            {
                request.RequestHandler.Credentials = this.Credentials;
            }

            // redirects
            request.RequestHandler.AllowAutoRedirect = this.FollowRedirects;
            if (this.FollowRedirects && this.MaxRedirects.HasValue)
            {
                request.RequestHandler.MaxAutomaticRedirections = this.MaxRedirects.Value;
            }

            ////////// 3. Configure HttpClient
            if (this.Timeout != 0)
            {
                request.RequestClient.Timeout = TimeSpan.FromMilliseconds(this.Timeout);
            }

            return request;
        }

        private long CalculateContentLength()
        {
            if (this.RequestBodyBytes != null)
            {
                return this.RequestBodyBytes.Length;
            }

            if (!this.HasFiles && !this.AlwaysMultipartFormData)
            {
                return this.Encoding.GetByteCount(this.RequestBody);
            }

            // calculate length for multipart form
            long length = 0;

            foreach (HttpFile file in this.Files)
            {
                length += this.Encoding.GetByteCount(GetMultipartFileHeader(file));
                length += file.ContentLength;
                length += this.Encoding.GetByteCount(LINE_BREAK);
            }

            length = this.Parameters.Aggregate(length,
                (current, param) => current + this.Encoding.GetByteCount(this.GetMultipartFormData(param)));

            length += this.Encoding.GetByteCount(GetMultipartFooter());

            return length;
        }
    }
}

