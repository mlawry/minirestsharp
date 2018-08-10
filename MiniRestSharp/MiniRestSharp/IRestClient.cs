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
using System.Threading;
using System.Threading.Tasks;

namespace MiniRestSharpCore
{
    public interface IRestClient
    {
        CookieContainer CookieContainer { get; set; }

        int? MaxRedirects { get; set; }

        string UserAgent { get; set; }

        int Timeout { get; set; }

        bool UseSynchronizationContext { get; set; }

        IAuthenticator Authenticator { get; set; }

        Uri BaseUrl { get; set; }

        Encoding Encoding { get; set; }

        IList<Parameter> DefaultParameters { get; }

        bool FollowRedirects { get; set; }

        Uri BuildUri(IRestRequest request);

        void AddHandler(string contentType, IDeserializer deserializer);

        void AddHandler(IEnumerable<string> contentTypes, IDeserializer deserializer);

        void RemoveHandler(string contentType);

        void ClearHandlers();

        Task<byte[]> DownloadDataTaskAsync(IRestRequest request);

        Task<IRestResponse> ExecuteTaskAsync(IRestRequest request);

    }
}
