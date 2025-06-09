using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TradeEngine
{
    internal class Program
    {
        /// <summary>
        /// The entry point of the Trade Engine application.
        /// Configures and starts the host, handles graceful shutdown, and retrieves necessary services.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        static async Task Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();

            Console.CancelKeyPress += (sender, eventargs) =>
            {
                Console.WriteLine($"[Error] {DateTime.UtcNow} :: Engine : Exiting Execution!");
                eventargs.Cancel = true;
                host.StopAsync().Wait();
            };

            await host.StartAsync();
            Console.WriteLine($"[Trace] {DateTime.UtcNow} :: Engine : Trade Execution Engine started.");

            // Retrieve the SocketIOService from the dependency injection container.
            var socketIOService = host.Services.GetRequiredService<SocketIOService>();

            await host.WaitForShutdownAsync();
        }

        /// <summary>
        /// Configures and registers the services required for the Trade Engine.
        /// This includes services for price simulation, monitoring, Socket.IO communication, and trade data storage.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        /// <returns>An IHostBuilder configured with the necessary services.</returns>
        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                // Clear default logging providers
                logging.ClearProviders();
            })
            .ConfigureServices((hostContext, services) =>
            {
                // regiser services
                services.AddSingleton<PriceSimulationService>();
                services.AddHostedService(sp => sp.GetRequiredService<PriceSimulationService>());
                services.AddSingleton<MonitoringService>();
                services.AddHostedService(sp => sp.GetRequiredService<MonitoringService>());
                services.AddSingleton<SocketIOService>();
                services.AddSingleton<TradeRepository>();
            });
    }
}
