
namespace MiniRestSharpCore
{
    /// <summary>
    /// Implement this interface to provide your own implementation of <see cref="IHttp"/>.
    /// </summary>
    public interface IHttpFactory
    {
        /// <summary>
        /// This method is called per HTTP request (i.e. when <see cref="IRestClient.ExecuteTaskAsync(IRestRequest)"/>
        /// is called). If sharing the same object across multiple requests then thread safety needs to be taken into account.
        /// </summary>
        IHttp Create();
    }
}
