using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNet6Test.Handlers
{
    internal class HandlerDefault
    {
        IConfiguration _configuration;
        IHttpClientFactory _httpClientFactory;

        public HandlerDefault(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(int statuscode, string responseBody)> HandleHttpRequest(Microsoft.AspNetCore.Http.HttpContext httpContext, byte[] bodyBytes, string debugMessage, CancellationToken stoppingToken)
        {
            string reportBaseUrl = _configuration["REPORT_BASE_URL"];
            await PerformHttp(HttpMethod.Post, $"{reportBaseUrl}/AspNet6TestHandler1_Request?url={WebUtility.UrlEncode(httpContext.Request.Path.Value)}", Encoding.UTF8.GetBytes(debugMessage), stoppingToken);

            string response = "I'm OK";
            int statuscode = 200;

            var byteContent = Encoding.UTF8.GetBytes(response);
            await PerformHttp(HttpMethod.Post, $"{reportBaseUrl}/AspNet6TestHandler1_Response?statuscode={statuscode}", byteContent, stoppingToken);

            return new (statuscode, response);
        }

        async Task<(bool success, HttpStatusCode statusCode, string body)> PerformHttp(HttpMethod method, string url, byte[] byteContent, CancellationToken stoppingToken)
        {
            try
            {
                using (var client = _httpClientFactory.CreateClient())
                {
                    using (var requestMessage = new HttpRequestMessage(method, url))
                    {
                        requestMessage.Headers.TransferEncodingChunked = false;
                        if (byteContent != null)
                        {
                            requestMessage.Content = new System.Net.Http.ByteArrayContent(byteContent);
                        }
                        using (var httpResponseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, stoppingToken))
                        {
                            var statusCode = httpResponseMessage.StatusCode;
                            var success = statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.NoContent;
                            using (var ms = new MemoryStream())
                            {
                                await httpResponseMessage.Content.CopyToAsync(ms, stoppingToken);
                                ms.Flush();
                                var serverResponse = Encoding.UTF8.GetString(ms.ToArray());
                                return new(success, statusCode, serverResponse);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new(false, HttpStatusCode.InternalServerError, $"Failed to get {url}. Exception={ex.ToString()}");
            }
        }
    }
}
