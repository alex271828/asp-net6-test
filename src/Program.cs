using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNet6Test
{
    public class Program
    {
        public static Logger _logger;
        static IConfiguration _configuration;

        public static int ServicePort = 8080;
        public static int MaxRequestSize = 10000;

        internal static string InstanceName = System.Environment.MachineName;

        public static CancellationTokenSource _cancelToken = new CancellationTokenSource();

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, configBuilder) =>
                {
                    configBuilder.Sources.Clear();
                    configBuilder.AddEnvironmentVariables();
                    configBuilder.AddJsonFile("appSettingFile.json", false);

                    _configuration = configBuilder.Build();

                    var logFolder = _configuration["LOG_FOLDER"];
                    if (logFolder == null) logFolder = AppDomain.CurrentDomain.BaseDirectory;
                    _logger = new Logger(logFolder, nameof(AspNet6Test));
                })
                .ConfigureLogging((hostBuilderContext, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                })
                .ConfigureServices((appBuilder, services) =>
                {
                    var configuration = appBuilder.Configuration;

                    ServicePort = int.Parse(_configuration["SERVICE_PORT"]);
                    MaxRequestSize = int.Parse(_configuration["MAX_REQUEST_SIZE_BYTES"]);

                    services.AddHostedService<ServiceWorker>();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseKestrel(options =>
                    {
                        options.Limits.MaxRequestBodySize = MaxRequestSize;
                        options.AddServerHeader = false;

                        Console.WriteLine($"SERVICE_PORT={ServicePort}");
                        options.ListenAnyIP(ServicePort, builder =>
                        {
                        });
                    });
                });
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Exit("Console_CancelKeyPress()");
            e.Cancel = true;
        }

        public static void Exit(string message)
        {
            _cancelToken.Cancel();
            _logger.Log(message);
        }

        public static async Task Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            var host = CreateHostBuilder(args).Build();

            _logger.Log("Main() starting...");

            await host.RunAsync(_cancelToken.Token);

            _logger.Log("Main() exiting...");

            _logger.Close();
        }
    }
}