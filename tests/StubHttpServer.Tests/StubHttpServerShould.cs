using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace StubHttpServer.Tests
{
    [TestFixture]
    public class StubHttpServerShould
    {
        [Test]
        public async Task CanSetupAndReturnAnHttpResponseUsingRelativeUri()
        {
            using (var server = new StubHttpServer())
            using (var httpClient = new HttpClient())
            {
                server.SetupRoute("/hello").Get().ReturnsStatusCode(HttpStatusCode.OK).WithTextContent("HELLO WORLD");

                httpClient.BaseAddress = new Uri(server.Url);
                var response = await httpClient.GetAsync("/hello");

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                (await response.Content.ReadAsStringAsync()).Should().Be("HELLO WORLD");
            }
        }
        
        [Test]
        public async Task CanSetupAndReturnAnHttpResponseUsingAbsoluteUri()
        {
            using (var server = new StubHttpServer())
            using (var httpClient = new HttpClient())
            {
                server.SetupRoute("/hello").Get().ReturnsStatusCode(HttpStatusCode.OK).WithTextContent("HELLO WORLD");

                var endpoint = $"{server.Url}" + "/hello"; //server.Url contains trailing slash
                
                var response = await httpClient.GetAsync(endpoint);

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                (await response.Content.ReadAsStringAsync()).Should().Be("HELLO WORLD");
            }
        }
        
        [Test]
        public async Task CanMutateARoute()
        {
            using (var server = new StubHttpServer())
            using (var httpClient = new HttpClient())
            {
                server.SetupRoute("/hello").Get().ReturnsStatusCode(HttpStatusCode.OK).WithTextContent("HELLO WORLD");

                httpClient.BaseAddress = new Uri(server.Url);
                var response = await httpClient.GetAsync("/hello");

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                (await response.Content.ReadAsStringAsync()).Should().Be("HELLO WORLD");

                server.SetupRoute("/hello").Get().ReturnsStatusCode(HttpStatusCode.OK).WithTextContent("GOODBYE WORLD");
                response = await httpClient.GetAsync("/hello");

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                (await response.Content.ReadAsStringAsync()).Should().Be("GOODBYE WORLD");
            }
        }
        
        [Test]
        public async Task CanInvokeCallbackWhenRouteIsInvoked()
        {
            using (var server = new StubHttpServer())
            using (var httpClient = new HttpClient())
            {
                var wasCalled = false;

                server.SetupRoute("/hello")
                    .Get()
                    .ReturnsStatusCode(HttpStatusCode.OK)
                    .WhenInvoked(ctx => wasCalled = true)
                    .WithTextContent("HELLO WORLD");

                httpClient.BaseAddress = new Uri(server.Url);
                var response = await httpClient.GetAsync("/hello");

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                (await response.Content.ReadAsStringAsync()).Should().Be("HELLO WORLD");

                Assert.True(wasCalled, "expect callback to have been invoked.");
            }
        }

        [Test]
        public async Task CanSaveRepresentationOfIncomingRequests()
        {
            using (var server = new StubHttpServer())
            using (var httpClient = new HttpClient())
            {
                server.SetupRoute("/hello").Post().ReturnsStatusCode(HttpStatusCode.OK).WithTextContent("HELLO WORLD");

                var endpoint = $"{server.Url}" + "/hello"; //server.Url contains trailing slash
                await httpClient.PostAsync(endpoint, new StringContent("HELLO", Encoding.UTF8, "text/plain"));

                var request = server.RequestLogs.First();

                request.Method.Should().Be("POST");
                request.Body.Should().Be("HELLO");
                request.Url.ToString().Should().Be(endpoint);
                request.Headers["Content-Type"].Should().Be("text/plain; charset=utf-8");
            }
        }
        
        [Test]
        public async Task ResetsTheBodyStreamAfterReading()
        {
            using (var server = new StubHttpServer())
            using (var httpClient = new HttpClient())
            {
                Stream body = new MemoryStream();
                
                server.SetupRoute("/hello")
                    .Post()
                    .ReturnsStatusCode(HttpStatusCode.OK)
                    .WhenInvoked(ctx => ctx.Request.Body.CopyTo(body))
                    .WithTextContent("HELLO WORLD");

                var endpoint = $"{server.Url}" + "/hello"; //server.Url contains trailing slash
                await httpClient.PostAsync(endpoint, new StringContent("HELLO", Encoding.UTF8, "text/plain"));

                using (var reader = new StreamReader(body))
                {
                    body.Position = 0;
                    (await reader.ReadToEndAsync()).Should().Be("HELLO");
                }
            }
        }
    }
}
