# mini-restsharp

A cut down version of the [RestSharp](http://github.com/restsharp/RestSharp) API for .NET Core 1.1.

The code is based on commit e7c65df751427298cb59f5456bbf1f59967996be (27 Apr 2016) from the *master* branch.

The HTTP engine implementation uses .NET Core's [System.Net.HttpWebRequest](https://docs.microsoft.com/en-us/dotnet/core/api/system.net.httpwebrequest) and [System.Net.HttpWebResponse](https://docs.microsoft.com/en-us/dotnet/core/api/system.net.httpwebresponse) classes.

Major changes from the normal RestSharp API include:

* No default serializer or deserializer.

* You must implement your own ISerializer and IDeserializer to perform serialization.

* JSON and XML serialization specific code has been removed.

* Only supports one asynchronous `ExecuteTaskAsync` method.

* No support for proxy, pipelining, client certificates, PreAuthenticate and other advanced features.

* No FollowRedirects support. RestClient always behaves as though `RestClient.FollowRedirects` is false.

* Timeout and ReadWriteTimeout are not supported at the moment, although they are still available for use. Hopefully they are implemented in a future HttpWebRequest revision.
