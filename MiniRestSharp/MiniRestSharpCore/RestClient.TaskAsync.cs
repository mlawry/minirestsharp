using System;
using System.Threading.Tasks;

namespace MiniRestSharpCore
{
    public partial class RestClient
    {
        /// <summary>
        /// Executes the specified request and downloads the response data
        /// </summary>
        /// <param name="request">Request to execute</param>
        /// <returns>Response data</returns>
        public async Task<byte[]> DownloadDataTaskAsync(IRestRequest request)
        {
            IRestResponse response = await this.ExecuteTaskAsync(request);

            return response.RawBytes;
        }

        /// <summary>
        /// Executes the request and returns a response, authenticating if needed
        /// </summary>
        /// <param name="request">Request to be executed</param>
        /// <returns>RestResponse</returns>
        public virtual Task<IRestResponse> ExecuteTaskAsync(IRestRequest request)
        {
            string method = Enum.GetName(typeof(Method), request.Method);

            switch (request.Method)
            {
                case Method.POST:
                case Method.PUT:
                case Method.PATCH:
                case Method.MERGE:
                    return this.Execute(request, method, DoExecuteAsPost);

                default:
                    return this.Execute(request, method, DoExecuteAsGet);
            }
        }


        private async Task<IRestResponse> Execute(IRestRequest request, string httpMethod,
            Func<IHttp, string, Task<HttpResponse>> getResponse)
        {
            this.AuthenticateIfNeeded(this, request);

            IRestResponse response = new RestResponse();

            try
            {
                IHttp http = this.HttpFactory.Create();

                this.ConfigureHttp(request, http);

                HttpResponse rawResponse = await getResponse(http, httpMethod);
                response = ConvertToRestResponse(request, rawResponse);
                response.Request = request;
                response.Request.IncreaseNumAttempts();
            }
            catch (Exception ex)
            {
                response.ResponseStatus = ResponseStatus.Error;
                response.ErrorMessage = ex.Message;
                response.ErrorException = ex;
            }

            return response;
        }

        private static Task<HttpResponse> DoExecuteAsGet(IHttp http, string method)
        {
            return http.AsGetTaskAsync(method);
        }

        private static Task<HttpResponse> DoExecuteAsPost(IHttp http, string method)
        {
            return http.AsPostTaskAsync(method);
        }

    }
}

