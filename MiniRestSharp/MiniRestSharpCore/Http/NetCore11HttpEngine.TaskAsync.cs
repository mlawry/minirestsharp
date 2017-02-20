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
using System.Threading.Tasks;

namespace MiniRestSharpCore
{
    /// <summary>
    /// HttpWebRequest wrapper (sync methods)
    /// This class is based on RestSharp's Http.Sync.cs file.
    /// </summary>
    public partial class NetCore11HttpEngine
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

        private async Task<HttpResponse> GetStyleMethodInternal(string method)
        {
            HttpWebRequest webRequest = this.ConfigureWebRequest(method, this.Url);

            if (this.HasBody && (method == "DELETE" || method == "OPTIONS"))
            {
                webRequest.ContentType = this.RequestContentType;
                await this.WriteRequestBody(webRequest);
            }

            return await this.GetResponse(webRequest);
        }

        private async Task<HttpResponse> PostPutInternal(string method)
        {
            HttpWebRequest webRequest = this.ConfigureWebRequest(method, this.Url);

            this.PreparePostBody(webRequest);

            await this.WriteRequestBody(webRequest);
            return await this.GetResponse(webRequest);
        }

        partial void AddSyncHeaderActions()
        {
            this.restrictedHeaderActions.Add("Connection", (r, v) => {
                if (v.ToLowerInvariant().Contains("keep-alive"))
                {
                    //if a user sets the connection header explicitly to "keep-alive" then we set the field on HttpWebRequest
                    r.Headers["Connection"] = "keep-alive";
                }
                else
                {
                    //if "Connection" is specified as anything else, we turn off keep alive functions
                    r.Headers.Remove("Connection");
                }
            });
            this.restrictedHeaderActions.Add("Content-Length", (r, v) => r.Headers[HttpRequestHeader.ContentLength] = v);
            this.restrictedHeaderActions.Add("Expect", (r, v) => r.Headers[HttpRequestHeader.Expect] = v);
            this.restrictedHeaderActions.Add("If-Modified-Since", (r, v) => r.Headers[HttpRequestHeader.IfModifiedSince] = Convert.ToDateTime(v).ToString("r")); // RFC1123 pattern.
            this.restrictedHeaderActions.Add("Referer", (r, v) => r.Headers[HttpRequestHeader.Referer] = v);
            this.restrictedHeaderActions.Add("Transfer-Encoding", (r, v) => r.Headers[HttpRequestHeader.TransferEncoding] = v);
            this.restrictedHeaderActions.Add("User-Agent", (r, v) => r.Headers[HttpRequestHeader.UserAgent] = v);
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

        private async Task<HttpResponse> GetResponse(HttpWebRequest request)
        {
            HttpResponse response = new HttpResponse { ResponseStatus = ResponseStatus.None };

            try
            {
                HttpWebResponse webResponse = await GetRawResponse(request);

                this.ExtractResponseData(response, webResponse);
            }
            catch (Exception ex)
            {
                this.ExtractErrorResponse(response, ex);
            }

            return response;
        }

        private static async Task<HttpWebResponse> GetRawResponse(HttpWebRequest request)
        {
            try
            {
                return (HttpWebResponse) await request.GetResponseAsync();
            }
            catch (WebException ex)
            {
                // Check to see if this is an HTTP error or a transport error.
                // In cases where an HTTP error occurs ( status code >= 400 )
                // return the underlying HTTP response, otherwise assume a
                // transport exception (ex: connection timeout) and
                // rethrow the exception

                if (ex.Response is HttpWebResponse)
                {
                    return ex.Response as HttpWebResponse;
                }

                throw;
            }
        }

        private async Task WriteRequestBody(HttpWebRequest webRequest)
        {
            if (this.HasBody || this.HasFiles || this.AlwaysMultipartFormData)
            {
                webRequest.Headers[HttpRequestHeader.ContentLength] = this.CalculateContentLength().ToString();
            }

            using (Stream requestStream = await webRequest.GetRequestStreamAsync())
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

        // TODO: Try to merge the shared parts between ConfigureWebRequest and ConfigureAsyncWebRequest (quite a bit of code
        // TODO: duplication at the moment).
        private HttpWebRequest ConfigureWebRequest(string method, Uri url)
        {
            HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url);

            webRequest.UseDefaultCredentials = this.UseDefaultCredentials;
            //webRequest.ServicePoint.Expect100Continue = false;
            // .NET Core has no support for ServicePoint, but a new ContinueTimeout property instead.
            webRequest.ContinueTimeout = 2000; // If server does not send 100-Continue within this time, we send POST entity-body anyway.

            this.AppendHeaders(webRequest);
            this.AppendCookies(webRequest);

            webRequest.Method = method;

            // make sure Content-Length header is always sent since default is -1
            if (!this.HasFiles && !this.AlwaysMultipartFormData)
            {
                webRequest.Headers[HttpRequestHeader.ContentLength] = "0";
            }

            //webRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None;
            webRequest.Headers[HttpRequestHeader.Te] = "deflate, gzip, identity";

            if (this.UserAgent.HasValue())
            {
                webRequest.Headers[HttpRequestHeader.UserAgent] = this.UserAgent;
            }

            //if (this.Timeout != 0)
            //{
            //    webRequest.Timeout = this.Timeout;
            //}

            //if (this.ReadWriteTimeout != 0)
            //{
            //    webRequest.ReadWriteTimeout = this.ReadWriteTimeout;
            //}

            if (this.Credentials != null)
            {
                webRequest.Credentials = this.Credentials;
            }

            return webRequest;
        }


        private long CalculateContentLength()
        {
            if (this.RequestBodyBytes != null)
            {
                return this.RequestBodyBytes.Length;
            }

            if (!this.HasFiles && !this.AlwaysMultipartFormData)
            {
                return this.encoding.GetByteCount(this.RequestBody);
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

