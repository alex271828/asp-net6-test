using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNet6Test
{
    public class ServiceWorker : BackgroundService
    {
        static System.Text.UTF8Encoding utf8enc = new System.Text.UTF8Encoding(false, false);

        IConfiguration _configuration;

        public ServiceWorker(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //System.Diagnostics.Debug.WriteLine("ExecuteAsync");

                await Task.Delay(1000, stoppingToken);
            }

            Program.Exit("ExecuteAsync() Exiting");
        }

        static async Task WriteAsUTF8(Microsoft.AspNetCore.Http.HttpResponse response, string contentType, int statusCode, string message)
        {
            if (!string.IsNullOrEmpty(contentType)) response.ContentType = contentType;

            if (string.IsNullOrEmpty(message))
            {
                response.StatusCode = statusCode;
                response.ContentLength = 0;
                return;
            }

            response.StatusCode = statusCode;
            var resultBytes = System.Text.Encoding.UTF8.GetBytes(message);
            response.ContentLength = resultBytes.Length;

            await response.BodyWriter.WriteAsync(resultBytes);
        }

        public static async Task HandleHttpRequest(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            //System.Diagnostics.Debug.WriteLine("HandleHttpRequest");

            var requestInfo = new List<KeyValuePair<string, string>>();

            requestInfo.Add(new KeyValuePair<string, string>("TraceIdentifier", httpContext.TraceIdentifier));
            requestInfo.Add(new KeyValuePair<string, string>("Path", httpContext.Request.Path.Value));
            requestInfo.Add(new KeyValuePair<string, string>("QueryString", httpContext.Request.QueryString.Value));
            requestInfo.Add(new KeyValuePair<string, string>("Method", httpContext.Request.Method));

            foreach (var header in httpContext.Request.Headers)
            {
                if (header.Value.Count == 1)
                {
                    requestInfo.Add(new KeyValuePair<string, string>($"Headers_{header.Key}", header.Value[0]));
                }
                else
                {
                    for (int i = 0; i < header.Value.Count; i++)
                    {
                        requestInfo.Add(new KeyValuePair<string, string>($"Headers_{header.Key}_{i}", header.Value[i]));
                    }
                }
            }

            // read body
            byte[] bodyBytes = null;
            {
                using (var ms = new MemoryStream())
                {
                    await httpContext.Request.Body.CopyToAsync(ms);

                    await ms.FlushAsync();

                    bodyBytes = ms.ToArray();
                }

                // add body to logs if needed
                if (bodyBytes.Length > 0)
                {
                    StringBuilder body = new StringBuilder();
                    body.AppendLine();
                    body.AppendLine();

                    int bytesPerLine = 16;
                    int index = 0;
                    do
                    {
                        body.Append("  ");

                        for (int i = 0; i < bytesPerLine && index + i < bodyBytes.Length; i++)
                        {
                            body.Append(bodyBytes[index + i].ToString("X02"));
                            body.Append(" ");
                        }

                        int extraSpaces = 16 - bodyBytes.Length + index;
                        for (int i = 0; i < extraSpaces; i++)
                        {
                            body.Append("   ");
                        }

                        body.Append(" : ");

                        for (int i = 0; i < bytesPerLine && index + i < bodyBytes.Length; i++)
                        {
                            byte b = bodyBytes[index + i];
                            if (b < 127 && b > 31)
                            {
                                body.Append(utf8enc.GetString(bodyBytes, index + i, 1));
                            }
                            else
                            {
                                body.Append('.');
                                if (b != 0x0A && b != 0x0D)
                                {
                                    bodyBytes[index + i] = 0x2E;
                                }
                            }
                        }

                        body.AppendLine();
                        index += bytesPerLine;

                    } while (index < bodyBytes.Length);

                    body.AppendLine();
                    body.AppendLine(utf8enc.GetString(bodyBytes));

                    requestInfo.Add(new KeyValuePair<string, string>("Body", body.ToString()));
                }
            }

            string responseBody = null;
            int statuscode = 404;
            if (string.Equals(httpContext.Request.Path.Value, "/status", StringComparison.InvariantCultureIgnoreCase))
            {
                var response = await AspNet6Test.Handlers.Handler1.HandleHttpRequest(httpContext, bodyBytes);
                statuscode = response.statuscode;
                responseBody = response.responseBody;
            }

            Program._logger.Log("HandleHttpRequest", requestInfo);
            await WriteAsUTF8(httpContext.Response, "text/plain", statuscode, responseBody);
            return;
        }
    }
}
