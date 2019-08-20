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
using MiniRestSharpCore.Serializers;

namespace MiniRestSharpCore
{
    public interface IRestRequest
    {
        /// <summary>
        /// Always send a multipart/form-data request - even when no Files are present.
        /// </summary>
        bool AlwaysMultipartFormData { get; set; }

        /// <summary>
        /// Set this to write response to Stream rather than reading into memory.
        /// </summary>
        Action<Stream> ResponseWriter { get; set; }

        /// <summary>
        /// Container of all HTTP parameters to be passed with the request. 
        /// See AddParameter() for explanation of the types of parameters that can be passed
        /// </summary>
        List<Parameter> Parameters { get; }

        /// <summary>
        /// Container of all the files to be uploaded with the request.
        /// </summary>
        List<FileParameter> Files { get; }

        /// <summary>
        /// Determines what HTTP method to use for this request. Supported methods: GET, POST, PUT, DELETE, HEAD, OPTIONS
        /// Default is GET
        /// </summary>
        Method Method { get; set; }

        /// <summary>
        /// The Resource URL to make the request against.
        /// Tokens are substituted with UrlSegment parameters and match by name.
        /// Should not include the scheme or domain. Do not include leading slash.
        /// Combined with RestClient.BaseUrl to assemble final URL:
        /// {BaseUrl}/{Resource} (BaseUrl is scheme + domain, e.g. http://example.com)
        /// </summary>
        /// <example>
        /// // example for url token replacement
        /// request.Resource = "Products/{ProductId}";
        /// request.AddParameter("ProductId", 123, ParameterType.UrlSegment);
        /// </example>
        string Resource { get; set; }

        /// <summary>
        /// Serializer to use when writing request bodies (when the AddBody() method is called).
        /// This value is null by default, which will cause an error. You should assign your own implementation.
        /// This is not used when the request only contains binary file data added via AddFile() methods.
        /// </summary>
        ISerializer EntityBodySerializer { get; set; }

        /// <summary>
        /// In general you would not need to set this directly. Used by the NtlmAuthenticator. 
        /// </summary>
        ICredentials Credentials { get; set; }

        /// <summary>
        /// Timeout in milliseconds to be used for the request. This timeout value overrides a timeout set on the RestClient.
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// How many attempts were made to send this Request?
        /// </summary>
        /// <remarks>
        /// This Number is incremented each time the RestClient sends the request.
        /// Useful when using Asynchronous Execution with Callbacks
        /// </remarks>
        int Attempts { get; }

        /// <summary>
        /// Determine whether or not the "default credentials" (e.g. the user account under which the current process is running)
        /// will be sent along to the server. The default is false.
        /// </summary>
        bool UseDefaultCredentials { get; set; }

        /// <summary>
        /// <para>
        /// Returns a tuple if this request is to use a named <see cref="HttpClient"/>. Returns the name (Tuple.Item1) and
        /// associated client (Tuple.Item2). The name is required to find the client's associated <see cref="HttpClientHandler"/>.
        /// Returns null if a new <see cref="HttpClient"/> instance should be used for this request (old RestSharp behaviour).
        /// </para>
        /// <para>
        /// To set this property, call <see cref="UseNamedHttpClient(IHttpClientFactory, string)"/>.
        /// </para>
        /// </summary>
        Tuple<string, HttpClient> NamedHttpClient { get; }

        /// <summary>
        /// <para>
        /// The old RestSharp behaviour is to create a new <see cref="HttpClient"/> (and associated <see cref="HttpClientHandler"/>)
        /// for every HTTP request; however, doing so is not good from a resource re-use point of view. See for example
        /// https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        /// This method supports the (re-)use of named clients for .NET Core 2.1 and later.
        /// </para>
        /// <para>
        /// This method is expected to call <see cref="IHttpClientFactory.CreateClient(string)"/> passing in <paramref name="name"/>
        /// as the parameter. The named <see cref="HttpClient"/> returned by the method is expected to be returned by the
        /// <see cref="NamedHttpClient"/> property together with the <paramref name="name"/>.
        /// </para>
        /// </summary>
        /// <param name="httpClientFactory">Must not be null.</param>
        /// <param name="name">Must not be null.</param>
        void UseNamedHttpClient(IHttpClientFactory httpClientFactory, string name);

        /// <summary>
        /// Adds a file to the Files collection to be included with a POST or PUT request 
        /// (other methods do not support file uploads).
        /// </summary>
        /// <param name="name">The parameter name to use in the request</param>
        /// <param name="path">Full path to file to upload</param>
        /// <param name="contentType">The MIME type of the file to upload</param>
        /// <returns>This request</returns>
        IRestRequest AddFile(string name, string path, string contentType = null);

        /// <summary>
        /// Adds the bytes to the Files collection with the specified file name and content type
        /// </summary>
        /// <param name="name">The parameter name to use in the request</param>
        /// <param name="bytes">The file data</param>
        /// <param name="fileName">The file name to use for the uploaded file</param>
        /// <param name="contentType">The MIME type of the file to upload</param>
        /// <returns>This request</returns>
        IRestRequest AddFile(string name, byte[] bytes, string fileName, string contentType = null);

        /// <summary>
        /// Adds the bytes to the Files collection with the specified file name and content type
        /// </summary>
        /// <param name="name">The parameter name to use in the request</param>
        /// <param name="writer">A function that writes directly to the stream.  Should NOT close the stream.</param>
        /// <param name="fileName">The file name to use for the uploaded file</param>
        /// <param name="contentLength">The length (in bytes) of the file content.</param>
        /// <param name="contentType">The MIME type of the file to upload</param>
        /// <returns>This request</returns>
        IRestRequest AddFile(string name, Action<Stream> writer, string fileName, long contentLength, string contentType = null);

        /// <summary>
        /// Add bytes to the Files collection as if it was a file of specific type
        /// </summary>
        /// <param name="name">A form parameter name</param>
        /// <param name="bytes">The file data</param>
        /// <param name="filename">The file name to use for the uploaded file</param>
        /// <param name="contentType">Specific content type. Es: application/x-gzip </param>
        /// <returns></returns>
        IRestRequest AddFileBytes(string name, byte[] bytes, string filename, string contentType = "application/x-gzip");

        /// <summary>
        /// Serializes obj to format specified by EntityBodySerializer.
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>This request</returns>
        /// <exception cref="InvalidOperationException">If EntityBodySerializer is null.</exception>
        IRestRequest AddBody(object obj);

        /// <summary>
        /// Calls AddParameter() for all public, readable properties specified in the includedProperties list
        /// </summary>
        /// <example>
        /// request.AddObject(product, "ProductId", "Price", ...);
        /// </example>
        /// <param name="obj">The object with properties to add as parameters</param>
        /// <param name="includedProperties">The names of the properties to include</param>
        /// <returns>This request</returns>
        IRestRequest AddObject(object obj, params string[] includedProperties);

        /// <summary>
        /// Calls AddParameter() for all public, readable properties of obj
        /// </summary>
        /// <param name="obj">The object with properties to add as parameters</param>
        /// <returns>This request</returns>
        IRestRequest AddObject(object obj);

        /// <summary>
        /// Add the parameter to the request
        /// </summary>
        /// <param name="p">Parameter to add</param>
        /// <returns></returns>
        IRestRequest AddParameter(Parameter p);

        /// <summary>
        /// Adds a HTTP parameter to the request (QueryString for GET, DELETE, OPTIONS and HEAD; Encoded form for POST and PUT)
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        /// <returns>This request</returns>
        IRestRequest AddParameter(string name, object value);

        /// <summary>
        /// Adds a parameter to the request. There are five types of parameters:
        /// - GetOrPost: Either a QueryString value or encoded form value based on method
        /// - HttpHeader: Adds the name/value pair to the HTTP request's Headers collection
        /// - UrlSegment: Inserted into URL if there is a matching url token e.g. {AccountId}
        /// - Cookie: Adds the name/value pair to the HTTP request's Cookies collection
        /// - RequestBody: Used by AddBody() (not recommended to use directly)
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        /// <param name="type">The type of parameter to add</param>
        /// <returns>This request</returns>
        IRestRequest AddParameter(string name, object value, ParameterType type);

        /// <summary>
        /// Adds a parameter to the request. There are five types of parameters:
        /// - GetOrPost: Either a QueryString value or encoded form value based on method
        /// - HttpHeader: Adds the name/value pair to the HTTP request's Headers collection
        /// - UrlSegment: Inserted into URL if there is a matching url token e.g. {AccountId}
        /// - Cookie: Adds the name/value pair to the HTTP request's Cookies collection
        /// - RequestBody: Used by AddBody() (not recommended to use directly)
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        /// <param name="contentType">Content-Type of the parameter</param>
        /// <param name="type">The type of parameter to add</param>
        /// <returns>This request</returns>
        IRestRequest AddParameter(string name, object value, string contentType, ParameterType type);

        /// <summary>
        /// Shortcut to AddParameter(name, value, HttpHeader) overload
        /// </summary>
        /// <param name="name">Name of the header to add</param>
        /// <param name="value">Value of the header to add</param>
        /// <returns></returns>
        IRestRequest AddHeader(string name, string value);

        /// <summary>
        /// Shortcut to AddParameter(name, value, Cookie) overload
        /// </summary>
        /// <param name="name">Name of the cookie to add</param>
        /// <param name="value">Value of the cookie to add</param>
        /// <returns></returns>
        IRestRequest AddCookie(string name, string value);

        /// <summary>
        /// Shortcut to AddParameter(name, value, UrlSegment) overload
        /// </summary>
        /// <param name="name">Name of the segment to add</param>
        /// <param name="value">Value of the segment to add</param>
        /// <returns></returns>
        IRestRequest AddUrlSegment(string name, string value);

        /// <summary>
        /// Shortcut to AddParameter(name, value, QueryString) overload
        /// </summary>
        /// <param name="name">Name of the parameter to add</param>
        /// <param name="value">Value of the parameter to add</param>
        /// <returns></returns>
        IRestRequest AddQueryParameter(string name, string value);

        /// <summary>
        /// A function to run prior to deserializing starting (e.g. change settings if error encountered)
        /// </summary>
        Action<IRestResponse> OnBeforeDeserialization { get; set; }

        /// <summary>
        /// Internal Method so that RestClient can increase the number of attempts
        /// </summary>
        void IncreaseNumAttempts();
    }
}
