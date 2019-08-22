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

using MiniRestSharpCore.Authenticators;
using MiniRestSharpCore.Deserializers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniRestSharpCore
{
    /// <summary>
    /// Client to translate RestRequests into Http requests and process response result
    /// </summary>
    public interface IRestClient
    {
        /// <summary>
        /// Authenticator to use for requests made by this client instance
        /// </summary>
        IAuthenticator Authenticator { get; set; }

        /// <summary>
        /// Combined with <see cref="IRestRequest.Resource"/> to construct URL for request
        /// Should include scheme and domain without trailing slash.
        /// </summary>
        /// <example>
        /// client.BaseUrl = new Uri("http://example.com");
        /// </example>
        Uri BaseUrl { get; set; }

        /// <summary>
        /// The CookieContainer used for requests made by this client instance
        /// </summary>
        CookieContainer CookieContainer { get; set; }

        /// <summary>
        /// Parameters included with every request made with this instance of <see cref="IRestClient"/>
        /// If specified in both client and request, the request wins
        /// </summary>
        IList<Parameter> DefaultParameters { get; }

        /// <summary>
        /// For requests that send data in text over HTTP, this is the encoding used to convert .NET strings
        /// into raw HTTP bytes. Also used to convert received bytes into a string e.g. in <see cref="IRestResponse.GetContent"/>.
        /// </summary>
        Encoding Encoding { get; set; }
        
        /// <summary>
        /// Default is true. Determine whether or not requests that result in 
        /// HTTP status codes of 3xx should follow returned redirect
        /// </summary>
        bool FollowRedirects { get; set; }

        /// <summary>
        /// Maximum number of redirects to follow if FollowRedirects is true
        /// </summary>
        int? MaxRedirects { get; set; }

        /// <summary>
        /// Timeout in milliseconds to use for requests made by this client instance
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// UserAgent to use for requests made by this client instance
        /// </summary>
        string UserAgent { get; set; }

        /// <summary>
        /// Whether to invoke async callbacks using the SynchronizationContext.Current captured when invoked
        /// </summary>
        bool UseSynchronizationContext { get; set; }

        /// <summary>
        /// Convenience method to get back the <see cref="Authenticator"/> as a typed instance. Will throw
        /// <see cref="InvalidCastException"/> if type is incorrect. Returns null if no Authenticator assigned.
        /// </summary>
        TAuthImpl GetAuthenticator<TAuthImpl>() where TAuthImpl : IAuthenticator;

        /// <summary>
        /// Assembles URL to call based on parameters, method and resource
        /// </summary>
        /// <param name="request">RestRequest to execute</param>
        /// <returns>Assembled System.Uri</returns>
        Uri BuildUri(IRestRequest request);

        /// <summary>
        /// Registers a content handler to process response content
        /// </summary>
        /// <param name="contentType">MIME content type of the response content</param>
        /// <param name="deserializer">Deserializer to use to process content</param>
        void AddHandler(string contentType, IDeserializer deserializer);

        /// <summary>
        /// Registers multiple content handlers to process response content using the same <paramref name="deserializer"/>.
        /// </summary>
        /// <param name="contentTypes">MIME content types of the response content</param>
        /// <param name="deserializer">Deserializer to use to process content</param>
        void AddHandler(IEnumerable<string> contentTypes, IDeserializer deserializer);

        /// <summary>
        /// Remove a content handler for the specified MIME content type
        /// </summary>
        /// <param name="contentType">MIME content type to remove</param>
        void RemoveHandler(string contentType);

        /// <summary>
        /// Remove all content handlers
        /// </summary>
        void ClearHandlers();

        /// <summary>
        /// Executes the specified request and downloads the response data
        /// </summary>
        /// <param name="request">Request to execute</param>
        /// <returns>Response data</returns>
        Task<byte[]> DownloadDataTaskAsync(IRestRequest request);

        /// <summary>
        /// Executes the request and returns a response, authenticating if needed
        /// </summary>
        /// <param name="request">Request to be executed</param>
        /// <returns>RestResponse</returns>
        Task<IRestResponse> ExecuteTaskAsync(IRestRequest request);

    }
}
