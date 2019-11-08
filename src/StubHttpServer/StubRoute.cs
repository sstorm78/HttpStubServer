using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace StubHttpServer
{
    public class StubRoute
    {
        public string HttpMethod { get; set; }

        public string Endpoint { get; set; }

        public TimeSpan Delay { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public string ResponseContent { get; set; }

        public Encoding ResponsEncoding { get; set; }

        public string ResponseContentType { get; set; }

        public Action<HttpContext> OnInvokedCallback { get; set; }

        public RequestDelegate RequestHandler
        {
            get
            {
                return async context =>
                       {
                           if (Delay > TimeSpan.Zero)
                           {
                               await Task.Delay(Delay);
                           }

                           context.Response.StatusCode = (int)StatusCode;
                           context.Response.ContentType = ResponseContentType;

                           if (!string.IsNullOrEmpty(ResponseContent))
                           {
                               await context.Response.WriteAsync(ResponseContent, ResponsEncoding);
                           }

                           OnInvokedCallback?.Invoke(context);
                       };
            }
        }
    }

}
