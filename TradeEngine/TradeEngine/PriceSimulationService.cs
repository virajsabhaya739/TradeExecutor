using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeEngine
{
    /// <summary>
    /// A background service that simulates price fluctuations for trading symbols.
    /// </summary>
    internal class PriceSimulationService : BackgroundService
    {
        private Dictionary<string, double> _prices = new Dictionary<string, double> { { "BTCUSD", 68000 } };  // Initial dummy prices

        /// <summary>
        /// Executes the price simulation logic asynchronously.
        /// </summary>
        /// <param name="stoppingToken">A token to signal when the service should stop.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var key in _prices.Keys.ToList())
                {
                    // Simulate a random price fluctuation.
                    _prices[key] += new Random().NextDouble() * 100 - 50;
                    Console.WriteLine($"{DateTime.UtcNow} :: PriceSimulationService: New Price for {key} = {GetLatestPrice(key)}");
                }

                // Wait for 5 seconds before the next simulation cycle.
                await Task.Delay(5000, stoppingToken);
            }
        }

        /// <summary>
        /// Gets the latest simulated price for a given symbol.
        /// </summary>
        /// <param name="symbol">The trading symbol.</param>
        /// <returns>The latest price for the symbol, or 0 if the symbol is not tracked.</returns>
        public double GetLatestPrice(string symbol)
        {
            return _prices.TryGetValue(symbol, out double price) ? price : 0;
        }
    }
}
