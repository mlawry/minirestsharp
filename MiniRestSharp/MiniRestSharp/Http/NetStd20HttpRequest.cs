using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MiniRestSharpCore.Http
{
    /// <summary>
    /// Equivalent to HttpWebRequest.
    /// </summary>
    public class NetStd20HttpRequest
    {
        /// <summary>
        /// For use by subclasses.
        /// </summary>
        protected NetStd20HttpRequest()
        {

        }


        /// <summary>
        /// Assigns a new <see cref="HttpClientHandler"/> instance to <see cref="RequestHandler"/> and
        /// creates a new <see cref="HttpClient"/> using the <see cref="HttpClient(HttpMessageHandler)"/>
        /// constructor, passing in the <see cref="HttpClientHandler"/> instance as the constructor parameter.
        /// Initialises a new <see cref="HttpRequestMessage"/> as <see cref="RequestMessage"/> as well.
        /// </summary>
        public NetStd20HttpRequest(string method, Uri url)
        {
            this.RequestHandler = new HttpClientHandler();
            this.RequestClient = new HttpClient(this.RequestHandler);
            this.RequestMessage = new HttpRequestMessage(new HttpMethod(method), url);

            // Must set a HttpContent here to allow HttpContentHeader to be set.
            this.RequestMessage.Content = new ByteArrayContent(new byte[0]);
        }


        public HttpClient RequestClient { get; protected set; }
        public HttpClientHandler RequestHandler { get; protected set; }
        public HttpRequestMessage RequestMessage { get; protected set; }


        /// <summary>
        /// Returns the underlying MemoryStream that represents the entity-body.
        /// </summary>
        public Stream GetRequestStreamAsync()
        {
            return new InMemoryRequestStream(this.RequestMessage);
        }


        /// <summary>
        /// Performs a HTTP action to get the response to this request.
        /// </summary>
        /// <returns></returns>
        public async Task<NetStd20HttpResponse> GetResponseAsync()
        {
            // Since most people will use IRestResponse.GetContent(), we should read the entire response including entity-body.
            Task<HttpResponseMessage> sendRequestTask =
                this.RequestClient.SendAsync(this.RequestMessage, HttpCompletionOption.ResponseContentRead);
            HttpResponseMessage responseMessage = await sendRequestTask.ConfigureAwait(false);

            var response = new NetStd20HttpResponse(responseMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new WebException(
                    string.Format("{0} {1}", (int)response.StatusCode, response.StatusDescription),
                    null,
                    WebExceptionStatus.ProtocolError,
                    response);
            }

            return response;
        }


        /// <summary>
        /// A helper class to cache entity-body content in memory and write to the given
        /// HttpRequestMessage when this stream is disposed.
        /// </summary>
        private class InMemoryRequestStream : MemoryStream
        {
            internal InMemoryRequestStream(HttpRequestMessage requestMessage) : base(4096)
            {
                if (requestMessage == null)
                {
                    throw new ArgumentNullException(nameof(requestMessage));
                }
                this.RequestMessage = requestMessage;
            }

            public HttpRequestMessage RequestMessage { get; private set; }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                // As part of disposing this stream, we have to write the bytes to the request message.
                var newHttpContent = new ByteArrayContent(ToArray());

                // But first copy over the headers because they may have already been assigned to the existing content.
                // (Luckily HttpContent only has 1 property we have to copy, just the Headers.)
                CopyContentHeaders(this.RequestMessage?.Content?.Headers, newHttpContent.Headers);

                // Change the Content property.
                this.RequestMessage.Content = newHttpContent;

                if (disposing)
                {
                    this.RequestMessage = null; // release reference.
                }
            }


            private static void CopyContentHeaders(HttpContentHeaders fromHeaders, HttpContentHeaders toHeaders)
            {
                if (fromHeaders == null || toHeaders == null)
                {
                    return;
                }

                foreach (KeyValuePair<string, IEnumerable<string>> kvPair in fromHeaders)
                {
                    // Apparently you cannot use the multi-value Add() method to add single values.
                    int valueCount = kvPair.Value.Count();
                    if (valueCount == 1)
                    {
                        toHeaders.Add(kvPair.Key, kvPair.Value.First());
                    }
                    else if (valueCount > 1)
                    {
                        toHeaders.Add(kvPair.Key, kvPair.Value);
                    }
                }
            }
        }

    }
}
