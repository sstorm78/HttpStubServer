using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace StubHttpServer
{
    public sealed class StubHttpServer : IDisposable
    {
        private readonly IDictionary<string, StubRoute> _routes;
        private readonly IWebHost _webHost;
        private readonly IList<Request> _requests;

        /// <summary>
        /// The URL that this server is listening on.
        /// </summary>
        public string Url => ((IServerAddressesFeature)_webHost.ServerFeatures.ElementAt(0).Value).Addresses.ElementAt(0);

        /// <summary>
        /// Creates and starts a new stub HTTP server. Use the SetupRoute() method to setup one or more routes that return canned responses.
        /// </summary>
        public StubHttpServer()
        {
            _routes = new Dictionary<string, StubRoute>(StringComparer.OrdinalIgnoreCase);
            _requests = new List<Request>();

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            _webHost = BuildAndStartServer();
        }

        public IEnumerable<Request> RequestLogs => _requests;

        private IWebHost BuildAndStartServer()
        {
            // By setting the port number to 0 the TCP stack will assign the next available port.
            const string url = "http://127.0.0.1:0";

            var host = new WebHostBuilder()
                .UseKestrel()
                .Configure(app =>
                {
                    app.Run(HttpRequestHandler);
                });

            return host.Start(url);
        }

        private async Task HttpRequestHandler(HttpContext context)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var requestFullUrl = context.Request.Path + context.Request.QueryString.ToUriComponent();

            _requests.Add(new Request(context.Request));
            var route = _routes.ContainsKey(requestFullUrl) ? _routes[requestFullUrl] : null;

            if (route == null || !route.HttpMethod.Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"No [{context.Request.Method}] route was setup on this stub HTTP server for endpoint [{context.Request.Path}].");
            }
            else
            {
                await route.RequestHandler(context);
            }
        }

        /// <summary>
        /// Setup a new route on this stub HTTP server using a fluent API. You can setup as many routes as you need.
        /// </summary>
        public HttpMethodBuilder SetupRoute(string endpoint)
        {
            var route = new StubRoute
            {
                Endpoint = endpoint
            };

            return new HttpMethodBuilder(this, route);
        }

        private void AddRoute(StubRoute route)
        {
            _routes[route.Endpoint] = route;
        }

        public void Dispose()
        {
            _webHost.Dispose();
        }

        public class HttpMethodBuilder
        {
            private readonly StubHttpServer _stubServer;
            private readonly StubRoute _route;

            public HttpMethodBuilder(StubHttpServer stubServer, StubRoute route)
            {
                _stubServer = stubServer;
                _route = route;
            }

            public StatusCodeOrDelayBuilder Get()
            {
                _route.HttpMethod = "GET";
                return new StatusCodeOrDelayBuilder(_stubServer, _route);
            }

            public StatusCodeOrDelayBuilder Post()
            {
                _route.HttpMethod = "POST";
                return new StatusCodeOrDelayBuilder(_stubServer, _route);
            }

            public StatusCodeOrDelayBuilder Put()
            {
                _route.HttpMethod = "PUT";
                return new StatusCodeOrDelayBuilder(_stubServer, _route);
            }

            public StatusCodeOrDelayBuilder Delete()
            {
                _route.HttpMethod = "DELETE";
                return new StatusCodeOrDelayBuilder(_stubServer, _route);
            }

            public StatusCodeOrDelayBuilder Head()
            {
                _route.HttpMethod = "HEAD";
                return new StatusCodeOrDelayBuilder(_stubServer, _route);
            }

            public StatusCodeOrDelayBuilder Options()
            {
                _route.HttpMethod = "OPTIONS";
                return new StatusCodeOrDelayBuilder(_stubServer, _route);
            }
        }

        public class StatusCodeOrDelayBuilder
        {
            private readonly StubHttpServer _stubServer;
            private readonly StubRoute _route;

            public StatusCodeOrDelayBuilder(StubHttpServer stubServer, StubRoute route)
            {
                _stubServer = stubServer;
                _route = route;
            }

            /// <summary>
            /// Configures this route to delay for the provided amount of time before returning a response.
            /// </summary>
            public StatusCodeAfterDelayBuilder DelayFor(TimeSpan delay)
            {
                _route.Delay = delay;
                return new StatusCodeAfterDelayBuilder(_stubServer, _route);
            }

            public ResponseContentBuilder ReturnsStatusCode(HttpStatusCode statusCode)
            {
                _route.StatusCode = statusCode;
                return new ResponseContentBuilder(_stubServer, _route);
            }
        }

        public class StatusCodeAfterDelayBuilder
        {
            private readonly StubHttpServer _stubServer;
            private readonly StubRoute _route;

            public StatusCodeAfterDelayBuilder(StubHttpServer stubServer, StubRoute route)
            {
                _stubServer = stubServer;
                _route = route;
            }

            public ResponseContentBuilder ThenReturnsStatusCode(HttpStatusCode statusCode)
            {
                _route.StatusCode = statusCode;
                return new ResponseContentBuilder(_stubServer, _route);
            }
        }

        public class ResponseContentBuilder
        {
            private readonly StubHttpServer _stubServer;
            private readonly StubRoute _route;

            public ResponseContentBuilder(StubHttpServer stubServer, StubRoute route)
            {
                _stubServer = stubServer;
                _route = route;
            }

            public StubHttpServer WithNoContent()
            {
                _stubServer.AddRoute(_route);
                return _stubServer;
            }

            public StubHttpServer WithTextContent(string text)
            {
                _route.ResponsEncoding = Encoding.UTF8;
                _route.ResponseContent = text;
                _route.ResponseContentType = "text/plain; charset=UTF-8";

                _stubServer.AddRoute(_route);
                return _stubServer;
            }

            public StubHttpServer WithJsonContent(string json)
            {
                _route.ResponsEncoding = Encoding.UTF8;
                _route.ResponseContent = json;
                _route.ResponseContentType = "application/json; charset=UTF-8";

                _stubServer.AddRoute(_route);
                return _stubServer;
            }

            public StubHttpServer WithJsonContent(object toSerialize)
            {
                var json = JsonConvert.SerializeObject(
                    toSerialize,
                    Formatting.Indented,
                    new JsonSerializerSettings());

                return WithJsonContent(json);
            }

            public ResponseContentBuilder WhenInvoked(Action<HttpContext> callback)
            {
                _route.OnInvokedCallback = callback;
                return this;
            }
        }
    }
}
