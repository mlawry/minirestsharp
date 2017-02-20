using System;
using System.Collections.Generic;
using System.Text;

namespace MiniRestSharpCore.Http
{
    internal class HttpEngineFactory : IHttpFactory
    {
        public IHttp Create()
        {
            return new NetCore11HttpEngine();
        }
    }
}
