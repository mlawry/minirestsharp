# mini-restsharp

A cut down version of the [RestSharp](http://github.com/restsharp/RestSharp) API for .NET Core 1.1.

The code is based on commit e7c65df751427298cb59f5456bbf1f59967996be (27 Apr 2016) from the *master* branch.

The HTTP engine implementation uses .NET Core's
[System.Net.Http.HttpClient](https://docs.microsoft.com/en-us/dotnet/core/api/system.net.http.httpclient) and
[System.Net.Http.HttpClientHandler](https://docs.microsoft.com/en-us/dotnet/core/api/system.net.http.httpclienthandler) classes.

Major changes from the normal RestSharp API include:

* No default serializer or deserializer.

* You must implement your own ISerializer and IDeserializer to perform serialization.

* JSON and XML serialization specific code has been removed.

* Deserialization is done in IRestResponse, so you can make sure the request was successful first.

* Only supports one asynchronous `ExecuteTaskAsync` method.

* No support for proxy, pipelining, client certificates, PreAuthenticate and other advanced features.

* ReadWriteTimeout is not supported.

Here is the modified code using the example from RestSharp's [README.markdown](https://github.com/restsharp/RestSharp/blob/master/README.markdown)

```csharp
var client = new RestClient("http://example.com");

// MiniRestSharpCore: You'll have to write your own Authenticator.
// client.Authenticator = new HttpBasicAuthenticator(username, password);

// MiniRestSharpCore: This method is new, allows you to associate multiple content
// types with a deserializer in a single method call. Although you will have to
// write your own Deserializer.
client.AddHandler(RestClient.DefaultJsonContentTypes, new MyJsonDeserializer());

var request = new RestRequest("resource/{id}", Method.POST);
request.AddParameter("name", "value"); // adds to POST or URL querystring based on Method
request.AddUrlSegment("id", "123"); // replaces matching token in request.Resource

// MiniRestSharpCore: You'll have to write your own Serializer. This property is new.
request.EntityBodySerializer = new MyJsonSerializer();
request.AddBody(object); // MiniRestSharpCore: Will serialize object using MyJsonSerializer.

// add parameters for all properties on an object
request.AddObject(object);

// or just whitelisted properties
request.AddObject(object, "PersonId", "Name", ...);

// easily add HTTP Headers
request.AddHeader("header", "value");

// add files to upload (works with compatible verbs)
request.AddFile("file", path);

// execute the request
IRestResponse response = await client.ExecuteTaskAsync(request); // MiniRestSharpCore: TaskAsync style call.
//var content = response.Content; // raw content as string
var content = response.GetContent(); // MiniRestSharpCore: API change here.

// or automatically deserialize result
// return content type is sniffed but can be explicitly set via RestClient.AddHandler();
//IRestResponse<Person> response2 = client.Execute<Person>(request);
//var name = response2.Data.Name;

// MiniRestSharpCore: No need for another call to client.Execute<>(), just re-use response.
// Will deserialize using the IDeserializers set via RestClient.AddHandler();
Person personObject = response.GetContent<Person>();
var name = personObject.Data.Name;

// or download and save file to disk
//client.DownloadData(request).SaveAs(path);
byte[] dataArray = await client.DownloadDataTaskAsync(request); // MiniRestSharpCore: TaskAsync style call.

// MiniRestSharpCore: No support for the following async calls.
// easy async support
//client.ExecuteAsync(request, response => {
//    Console.WriteLine(response.Content);
//});

// async with deserialization
//var asyncHandle = client.ExecuteAsync<Person>(request, response => {
//    Console.WriteLine(response.Data.Name);
//});

// abort the request on demand
//asyncHandle.Abort();
```
