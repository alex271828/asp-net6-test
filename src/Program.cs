using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace AspNet6Test
{
    public class Program
    {
        public static Logger _logger;
        static IConfiguration _configuration;

        public static int ServicePort = 8080;
        public static int ServicePortSSL = 8443;
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
                    configBuilder.AddJsonFile($"appSettingFile.{Environment.MachineName}.json", true);

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

                    services.AddHttpClient();

                    ServicePort = int.Parse(_configuration["SERVICE_PORT"]);
                    ServicePortSSL = int.Parse(_configuration["SERVICE_PORT_SSL"]);
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

                        Console.WriteLine($"SERVICE_PORT={ServicePortSSL}");
                        options.ListenAnyIP(ServicePortSSL, builder =>
                        {
                            builder.UseHttps(GetSelfSignedCertificate());
                        });
                    });
                });
        }

        private static X509Certificate2 GetSelfSignedCertificate()
        {
            var password = Guid.NewGuid().ToString();
            var commonName = nameof(AspNet6Test);
            var rsaKeySize = 2048;
            var years = 5;
            var hashAlgorithm = HashAlgorithmName.SHA256;

            using (var rsa = RSA.Create(rsaKeySize))
            {
                var request = new CertificateRequest($"cn={commonName}", rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                  new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false)
                );
                request.CertificateExtensions.Add(
                  new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false)
                );

                var certificate = request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(years));
                //certificate.FriendlyName = commonName;

                // Return the PFX exported version that contains the key
                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, password), password, X509KeyStorageFlags.MachineKeySet);
            }
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