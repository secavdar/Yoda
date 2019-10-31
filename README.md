# Yoda FX ![Build Status](https://ci.appveyor.com/api/projects/status/github/secavdar/yoda?branch=master&svg=true)

Yoda is a lightweight, memory friendly API Framework which is based on .Net Core 2.2. The goal of the framework is provide a better memory allocation and CPU usage for microservices and APIs which are mainly run in docker containers.

Yoda is designed to be lightweight and configurable in every steps of Http requests and responses.

Write your application like ASP.Net Core, with less libraries and less resources.

```csharp
public class TestController : ControllerBase
{
    [HttpGet]
    [Route("Hello/{name}")]
    public IHttpResponse Get(string name)
    {
        return Ok($"Hello, { name }");
    }
}
```
