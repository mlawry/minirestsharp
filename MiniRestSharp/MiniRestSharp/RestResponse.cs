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
using System.Diagnostics;
using System.Net;
using MiniRestSharpCore.Extensions;
using MiniRestSharpCore.Deserializers;

namespace MiniRestSharpCore
{

    /// <summary>
    /// Base class for common properties shared by RestResponse and RestResponse[[T]]
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay()}")]
    public abstract class RestResponseBase
    {
        private string content;

        /// <summary>
        /// Default constructor
        /// </summary>
        protected RestResponseBase()
        {
            this.ResponseStatus = ResponseStatus.None;
            this.Headers = new List<Parameter>();
            this.Cookies = new List<RestResponseCookie>();
        }

        /// <summary>
        /// The RestRequest that was made to get this RestResponse
        /// </summary>
        /// <remarks>
        /// Mainly for debugging if ResponseStatus is not OK
        /// </remarks> 
        public IRestRequest Request { get; set; }

        /// <summary>
        /// MIME content type of response
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Length in bytes of the response content
        /// </summary>
        public long ContentLength { get; set; }

        /// <summary>
        /// Encoding of the response content
        /// </summary>
        public string ContentEncoding { get; set; }

        /// <summary>
        /// HTTP response status code
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Description of HTTP status returned
        /// </summary>
        public string StatusDescription { get; set; }

        /// <summary>
        /// Response content
        /// </summary>
        public byte[] RawBytes { get; set; }

        /// <summary>
        /// The URL that actually responded to the content (different from request if redirected)
        /// </summary>
        public Uri ResponseUri { get; set; }

        /// <summary>
        /// HttpWebResponse.Server
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Cookies returned by server with the response
        /// </summary>
        public IList<RestResponseCookie> Cookies { get; protected internal set; }

        /// <summary>
        /// Headers returned by server with the response
        /// </summary>
        public IList<Parameter> Headers { get; protected internal set; }

        /// <summary>
        /// Status of the request. Will return Error for transport errors.
        /// HTTP errors will still return ResponseStatus.Completed, check StatusCode instead
        /// </summary>
        public ResponseStatus ResponseStatus { get; set; }

        /// <summary>
        /// Transport or other non-HTTP error generated while attempting request
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The exception thrown during the request, if any
        /// </summary>
        public Exception ErrorException { get; set; }

        /// <summary>
        /// Assists with debugging responses by displaying in the debugger output
        /// </summary>
        /// <returns></returns>
        protected string DebuggerDisplay()
        {
            return string.Format("StatusCode: {0}, Content-Type: {1}, Content-Length: {2})",
                this.StatusCode, this.ContentType, this.ContentLength);
        }


        /// <summary>
        /// Get a string representation of response content.
        /// Attempts to convert RawBytes into a string, using its byte order mark to determine the right encoding.
        /// If no byte order mark then uses UTF-8.
        /// 
        /// Note coversion is only done the first time, the resulting string is cached and returned on subsequent calls.
        /// </summary>
        public string GetContent()
        {
            return this.content ?? (this.content = this.RawBytes.AsString());
        }
    }

    /// <summary>
    /// Container for data sent back from API
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay()}")]
    public class RestResponse : RestResponseBase, IRestResponse
    {
        /// <summary>
        /// The deserializer to use in GetContent[T]().
        /// </summary>
        public IDeserializer ContentDeserializer { get; set; }


        /// <summary>
        /// Use ContentDeserializer to convert this response into a typed object.
        /// If ContentDeserializer is null then returns default(T), which is null for reference types.
        /// Note the ContentDeserializer may also return null.
        /// </summary>
        public T GetContent<T>()
        {
            // Only continue if there is a handler defined else there is no way to deserialize the data.
            // This can happen when a request returns for example a 404 page instead of the requested JSON/XML resource
            if (this.ContentDeserializer != null)
            {
                return this.ContentDeserializer.Deserialize<T>(this);
            }

            return default(T);
        }

    }
}
