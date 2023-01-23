using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNet6Test.Handlers
{
    internal class Handler1
    {
        public static async Task<(int statuscode, string responseBody)> HandleHttpRequest(Microsoft.AspNetCore.Http.HttpContext httpContext, byte[] bodyBytes)
        {
            return new (200, "I'm OK");
        }
    }
}
