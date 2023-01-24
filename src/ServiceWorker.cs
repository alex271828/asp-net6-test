using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNet6Test
{
    public class ServiceWorker : BackgroundService
    {
        IConfiguration _configuration;
        IHttpClientFactory _httpClientFactory;

        public ServiceWorker(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
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
    }
}
