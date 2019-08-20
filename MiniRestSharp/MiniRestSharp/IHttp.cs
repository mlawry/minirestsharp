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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MiniRestSharpCore
{
    public interface IHttp
    {
        Action<Stream> ResponseWriter { get; set; }

        CookieContainer CookieContainer { get; set; }

        ICredentials Credentials { get; set; }

        /// <summary>
        /// Always send a multipart/form-data request - even when no Files are present.
        /// </summary>
        bool AlwaysMultipartFormData { get; set; }

        string UserAgent { get; set; }

        int Timeout { get; set; }

        bool FollowRedirects { get; set; }

        int? MaxRedirects { get; set; }

        bool UseDefaultCredentials { get; set; }

        Encoding Encoding { get; set; }

        IList<HttpHeader> Headers { get; }

        IList<HttpParameter> Parameters { get; }

        IList<HttpFile> Files { get; }

        IList<HttpCookie> Cookies { get; }

        string RequestBody { get; set; }

        string RequestContentType { get; set; }

        /// <summary>
        /// An alternative to RequestBody, for when the caller already has the byte array.
        /// </summary>
        byte[] RequestBodyBytes { get; set; }

        Uri Url { get; set; }

        Task<HttpResponse> AsPostTaskAsync(string httpMethod);

        Task<HttpResponse> AsGetTaskAsync(string httpMethod);

        /// <summary>
        /// <para>
        /// The old RestSharp behaviour is to create a new <see cref="HttpClient"/> (and associated <see cref="HttpClientHandler"/>)
        /// for every HTTP request; however, doing so is not good from a resource re-use point of view. See for example
        /// https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        /// This method supports the (re-)use of named clients for .NET Core 2.1 and later.
        /// </para>
        /// <para>
        /// This method is expected to remember the named client set and use it to execute all future <see cref="IRestRequest"/>s.
        /// </para>
        /// </summary>
        /// <param name="name">
        /// The name of the named client, used to match up with the related <see cref="HttpClientHandler"/>. Must not be null.
        /// </param>
        /// <param name="httpClient">The named client itself. Must not be null.</param>
        void SetNamedHttpClient(string name, HttpClient httpClient);
    }
}
