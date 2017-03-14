using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MiniRestSharpCore.Http
{
    /// <summary>
    /// Equivalent to HttpWebRequest.
    /// </summary>
    internal class NetCore11HttpRequest
    {
        internal NetCore11HttpRequest(string method, Uri url)
        {
            this.RequestHandler = new HttpClientHandler();
            this.RequestClient = new HttpClient(this.RequestHandler);
            this.RequestMessage = new HttpRequestMessage(new HttpMethod(method), url);

            // Must set a HttpContent here to allow HttpContentHeader to be set.
            // Use StreamContent with MemoryStream to allow the entity-body to be added later via a stream.
            this.EntityBodyStream = new MemoryStream(4096);
            this.RequestMessage.Content = new StreamContent(this.EntityBodyStream);
        }


        internal HttpClient RequestClient { get; private set; }
        internal HttpClientHandler RequestHandler { get; private set; }
        internal HttpRequestMessage RequestMessage { get; private set; }

        private MemoryStream EntityBodyStream { get; set; }


        /// <summary>
        /// Returns the underlying MemoryStream that represents the entity-body.
        /// </summary>
        internal Stream GetRequestStreamAsync()
        {
            return this.EntityBodyStream;
        }


        /// <summary>
        /// Performs a HTTP action to get the response to this request.
        /// </summary>
        /// <returns></returns>
        internal async Task<NetCore11HttpResponse> GetResponseAsync()
        {
            // Since most people will use IRestResponse.GetContent(), we should read the entire response including entity-body.
            Task<HttpResponseMessage> sendRequestTask =
                this.RequestClient.SendAsync(this.RequestMessage, HttpCompletionOption.ResponseContentRead);
            HttpResponseMessage responseMessage = await sendRequestTask.ConfigureAwait(false);

            var response = new NetCore11HttpResponse(responseMessage);

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
    }
}
