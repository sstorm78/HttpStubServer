using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Internal;

namespace StubHttpServer
{
    public class Request
    {
        public string Method { get; }
        public Uri Url { get; }
        public IDictionary<string, string> Headers { get; }
        public string Body { get; }

        internal Request(HttpRequest request)
        {
            Method = request.Method;
            Url = new Uri(request.GetEncodedUrl());
            Headers = request.Headers.ToDictionary((x) => x.Key, (y) => y.Value.ToString());

            request.EnableRewind();

            var reader = new StreamReader(request.Body);
            Body = reader.ReadToEnd();
            request.Body.Position = 0;
        }
    }
}
