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

using MiniRestSharpCore.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MiniRestSharpCore
{
    /// <summary>
    /// Container for data used to make requests
    /// </summary>
    public class RestRequest : IRestRequest
    {
        /// <summary>
        /// Always send a multipart/form-data request - even when no Files are present.
        /// </summary>
        public bool AlwaysMultipartFormData { get; set; }

        /// <summary>
        /// Serializer to use when writing request bodies (when the AddBody() method is called).
        /// This value is null by default, which will cause an error. You should assign your own implementation.
        /// This is not used when the request only contains binary file data added via AddFile() methods.
        /// </summary>
        public ISerializer EntityBodySerializer { get; set; }

        /// <summary>
        /// Set this to write response to Stream rather than reading into memory.
        /// </summary>
        public Action<Stream> ResponseWriter { get; set; }

        /// <summary>
        /// Determine whether or not the "default credentials" (e.g. the user account under which the current process is running)
        /// will be sent along to the server. The default is false.
        /// </summary>
        public bool UseDefaultCredentials { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public RestRequest()
        {
            this.Method = Method.GET;
            this.Parameters = new List<Parameter>();
            this.Files = new List<FileParameter>();

            this.OnBeforeDeserialization = r => { };
        }

        /// <summary>
        /// Sets Method property to value of method
        /// </summary>
        /// <param name="method">Method to use for this request</param>
        public RestRequest(Method method) : this()
        {
            this.Method = method;
        }

        /// <summary>
        /// Sets Resource property
        /// </summary>
        /// <param name="resource">Resource to use for this request</param>
        public RestRequest(string resource) : this(resource, Method.GET) { }

        /// <summary>
        /// Sets Resource and Method properties
        /// </summary>
        /// <param name="resource">Resource to use for this request</param>
        /// <param name="method">Method to use for this request</param>
        public RestRequest(string resource, Method method) : this()
        {
            this.Resource = resource;
            this.Method = method;
        }

        /// <summary>
        /// Sets Resource property
        /// </summary>
        /// <param name="resource">Resource to use for this request</param>
        public RestRequest(Uri resource) : this(resource, Method.GET) { }

        /// <summary>
        /// Sets Resource and Method properties
        /// </summary>
        /// <param name="resource">Resource to use for this request</param>
        /// <param name="method">Method to use for this request</param>
        public RestRequest(Uri resource, Method method)
            : this(resource.IsAbsoluteUri
                ? resource.AbsolutePath + resource.Query
                : resource.OriginalString, method)
        {
            //resource.PathAndQuery not supported by Silverlight :(
        }

        /// <summary>
        /// Adds a file to the Files collection to be included with a POST or PUT request 
        /// (other methods do not support file uploads).
        /// </summary>
        /// <param name="name">The parameter name to use in the request</param>
        /// <param name="path">Full path to file to upload</param>
        /// <param name="contentType">The MIME type of the file to upload</param>
        /// <returns>This request</returns>
        public IRestRequest AddFile(string name, string path, string contentType = null)
        {
            FileInfo f = new FileInfo(path);
            long fileLength = f.Length;

            return this.AddFile(new FileParameter
            {
                Name = name,
                FileName = Path.GetFileName(path),
                ContentLength = fileLength,
                Writer = s =>
                {
                    using (StreamReader file = new StreamReader(new FileStream(path, FileMode.Open)))
                    {
                        file.BaseStream.CopyTo(s);
                    }
                },
                ContentType = contentType
            });
        }

        /// <summary>
        /// Adds the bytes to the Files collection with the specified file name
        /// </summary>
        /// <param name="name">The parameter name to use in the request</param>
        /// <param name="bytes">The file data</param>
        /// <param name="fileName">The file name to use for the uploaded file</param>
        /// <param name="contentType">The MIME type of the file to upload</param>
        /// <returns>This request</returns>
        public IRestRequest AddFile(string name, byte[] bytes, string fileName, string contentType = null)
        {
            return this.AddFile(FileParameter.Create(name, bytes, fileName, contentType));
        }

        /// <summary>
        /// Adds the bytes to the Files collection with the specified file name and content type
        /// </summary>
        /// <param name="name">The parameter name to use in the request</param>
        /// <param name="writer">A function that writes directly to the stream.  Should NOT close the stream.</param>
        /// <param name="fileName">The file name to use for the uploaded file</param>
        /// <param name="contentLength">The length (in bytes) of the file content.</param>
        /// <param name="contentType">The MIME type of the file to upload</param>
        /// <returns>This request</returns>
        public IRestRequest AddFile(string name, Action<Stream> writer, string fileName, long contentLength, string contentType = null)
        {
            return this.AddFile(new FileParameter
            {
                Name = name,
                Writer = writer,
                FileName = fileName,
                ContentLength = contentLength,
                ContentType = contentType
            });
        }

        private IRestRequest AddFile(FileParameter file)
        {
            this.Files.Add(file);

            return this;
        }

        /// <summary>
        /// Add bytes to the Files collection as if it was a file of specific type
        /// </summary>
        /// <param name="name">A form parameter name</param>
        /// <param name="bytes">The file data</param>
        /// <param name="filename">The file name to use for the uploaded file</param>
        /// <param name="contentType">Specific content type. Es: application/x-gzip </param>
        /// <returns></returns>
        public IRestRequest AddFileBytes(string name, byte[] bytes, string filename, string contentType = "application/x-gzip")
        {
            long length = bytes.Length;

            return this.AddFile(new FileParameter
            {
                Name = name,
                FileName = filename,
                ContentLength = length,
                ContentType = contentType,
                Writer = s =>
                {
                    using (StreamReader file = new StreamReader(new MemoryStream(bytes)))
                    {
                        file.BaseStream.CopyTo(s);
                    }
                }
            });
        }

        /// <summary>
        /// Serializes obj to format specified by EntityBodySerializer.
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>This request</returns>
        /// <exception cref="InvalidOperationException">If EntityBodySerializer is null.</exception>
        public IRestRequest AddBody(object obj)
        {
            if (this.EntityBodySerializer == null)
            {
                throw new InvalidOperationException("The EntityBodySerializer property cannot be null. It is required to serialize the 'obj' parameter.");
            }

            string serialized = this.EntityBodySerializer.Serialize(obj);
            string contentType = this.EntityBodySerializer.ContentType;

            // passing the content type as the parameter name because there can only be
            // one parameter with ParameterType.RequestBody so name isn't used otherwise
            // it's a hack, but it works :)
            return this.AddParameter(contentType, serialized, ParameterType.RequestBody);
        }

        /// <summary>
        /// Calls AddParameter() for all public, readable properties specified in the includedProperties list
        /// </summary>
        /// <example>
        /// request.AddObject(product, "ProductId", "Price", ...);
        /// </example>
        /// <param name="obj">The object with properties to add as parameters</param>
        /// <param name="includedProperties">The names of the properties to include</param>
        /// <returns>This request</returns>
        public IRestRequest AddObject(object obj, params string[] includedProperties)
        {
            // automatically create parameters from object props
            Type type = obj.GetType();
            PropertyInfo[] props = type.GetProperties();

            foreach (PropertyInfo prop in props)
            {
                bool isAllowed = includedProperties.Length == 0 ||
                                 (includedProperties.Length > 0 && includedProperties.Contains(prop.Name));

                if (!isAllowed)
                {
                    continue;
                }

                Type propType = prop.PropertyType;
                object val = prop.GetValue(obj, null);

                if (val == null)
                {
                    continue;
                }

                if (propType.IsArray)
                {
                    Type elementType = propType.GetElementType();

                    if (((Array)val).Length > 0 &&
                        elementType != null &&
                        (elementType.GetTypeInfo().IsPrimitive || elementType.GetTypeInfo().IsValueType || elementType == typeof(string)))
                    {
                        // convert the array to an array of strings
                        string[] values = (from object item in ((Array)val)
                                           select item.ToString()).ToArray<string>();

                        val = string.Join(",", values);
                    }
                    else
                    {
                        // try to cast it
                        val = string.Join(",", (string[])val);
                    }
                }

                this.AddParameter(prop.Name, val);
            }

            return this;
        }

        /// <summary>
        /// Calls AddParameter() for all public, readable properties of obj
        /// </summary>
        /// <param name="obj">The object with properties to add as parameters</param>
        /// <returns>This request</returns>
        public IRestRequest AddObject(object obj)
        {
            this.AddObject(obj, new string[] { });

            return this;
        }

        /// <summary>
        /// Add the parameter to the request
        /// </summary>
        /// <param name="p">Parameter to add</param>
        /// <returns></returns>
        public IRestRequest AddParameter(Parameter p)
        {
            this.Parameters.Add(p);

            return this;
        }

        /// <summary>
        /// Adds a HTTP parameter to the request (QueryString for GET, DELETE, OPTIONS and HEAD; Encoded form for POST and PUT)
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        /// <returns>This request</returns>
        public IRestRequest AddParameter(string name, object value)
        {
            return this.AddParameter(new Parameter
            {
                Name = name,
                Value = value,
                Type = ParameterType.GetOrPost
            });
        }

        /// <summary>
        /// Adds a parameter to the request. There are four types of parameters:
        /// - GetOrPost: Either a QueryString value or encoded form value based on method
        /// - HttpHeader: Adds the name/value pair to the HTTP request's Headers collection
        /// - UrlSegment: Inserted into URL if there is a matching url token e.g. {AccountId}
        /// - RequestBody: Used by AddBody() (not recommended to use directly)
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        /// <param name="type">The type of parameter to add</param>
        /// <returns>This request</returns>
        public IRestRequest AddParameter(string name, object value, ParameterType type)
        {
            return this.AddParameter(new Parameter
            {
                Name = name,
                Value = value,
                Type = type
            });
        }

        /// <summary>
        /// Adds a parameter to the request. There are four types of parameters:
        /// - GetOrPost: Either a QueryString value or encoded form value based on method
        /// - HttpHeader: Adds the name/value pair to the HTTP request's Headers collection
        /// - UrlSegment: Inserted into URL if there is a matching url token e.g. {AccountId}
        /// - RequestBody: Used by AddBody() (not recommended to use directly)
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        /// <param name="contentType">Content-Type of the parameter</param>
        /// <param name="type">The type of parameter to add</param>
        /// <returns>This request</returns>
        public IRestRequest AddParameter(string name, object value, string contentType, ParameterType type)
        {
            return this.AddParameter(new Parameter
            {
                Name = name,
                Value = value,
                ContentType = contentType,
                Type = type
            });
        }

        /// <summary>
        /// Shortcut to AddParameter(name, value, HttpHeader) overload
        /// </summary>
        /// <param name="name">Name of the header to add</param>
        /// <param name="value">Value of the header to add</param>
        /// <returns></returns>
        public IRestRequest AddHeader(string name, string value)
        {
            const string portSplit = @":\d+";
            Func<string, bool> invalidHost =
                host => Uri.CheckHostName(Regex.Split(host, portSplit)[0]) == UriHostNameType.Unknown;

            if (name == "Host" && invalidHost(value))
            {
                throw new ArgumentException("The specified value is not a valid Host header string.", "value");
            }

            return this.AddParameter(name, value, ParameterType.HttpHeader);
        }

        /// <summary>
        /// Shortcut to AddParameter(name, value, Cookie) overload
        /// </summary>
        /// <param name="name">Name of the cookie to add</param>
        /// <param name="value">Value of the cookie to add</param>
        /// <returns></returns>
        public IRestRequest AddCookie(string name, string value)
        {
            return this.AddParameter(name, value, ParameterType.Cookie);
        }

        /// <summary>
        /// Shortcut to AddParameter(name, value, UrlSegment) overload
        /// </summary>
        /// <param name="name">Name of the segment to add</param>
        /// <param name="value">Value of the segment to add</param>
        /// <returns></returns>
        public IRestRequest AddUrlSegment(string name, string value)
        {
            return this.AddParameter(name, value, ParameterType.UrlSegment);
        }

        /// <summary>
        /// Shortcut to AddParameter(name, value, QueryString) overload
        /// </summary>
        /// <param name="name">Name of the parameter to add</param>
        /// <param name="value">Value of the parameter to add</param>
        /// <returns></returns>
        public IRestRequest AddQueryParameter(string name, string value)
        {
            return this.AddParameter(name, value, ParameterType.QueryString);
        }

        /// <summary>
        /// Container of all HTTP parameters to be passed with the request. 
        /// See AddParameter() for explanation of the types of parameters that can be passed
        /// </summary>
        public List<Parameter> Parameters { get; private set; }

        /// <summary>
        /// Container of all the files to be uploaded with the request.
        /// </summary>
        public List<FileParameter> Files { get; private set; }

        /// <summary>
        /// Determines what HTTP method to use for this request. Supported methods: GET, POST, PUT, DELETE, HEAD, OPTIONS
        /// Default is GET
        /// </summary>
        public Method Method { get; set; }

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
        public string Resource { get; set; }

        /// <summary>
        /// A function to run prior to deserializing starting (e.g. change settings if error encountered)
        /// </summary>
        public Action<IRestResponse> OnBeforeDeserialization { get; set; }

        /// <summary>
        /// In general you would not need to set this directly. Used by the NtlmAuthenticator. 
        /// </summary>
        public ICredentials Credentials { get; set; }

        /// <summary>
        /// Timeout in milliseconds to be used for the request. This timeout value overrides a timeout set on the RestClient.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// The number of milliseconds before the writing or reading times out.  This timeout value overrides a timeout set on the RestClient.
        /// </summary>
        public int ReadWriteTimeout { get; set; }

        /// <summary>
        /// Internal Method so that RestClient can increase the number of attempts
        /// </summary>
        public void IncreaseNumAttempts()
        {
            this.Attempts++;
        }

        /// <summary>
        /// How many attempts were made to send this Request?
        /// </summary>
        /// <remarks>
        /// This Number is incremented each time the RestClient sends the request.
        /// Useful when using Asynchronous Execution with Callbacks
        /// </remarks>
        public int Attempts { get; private set; }
    }
}
