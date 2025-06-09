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
    /// A background service that monitors open trades and checks for Stop Loss or Target hits.
    /// </summary>
    internal class MonitoringService : BackgroundService
    {
        private readonly TradeRepository _tradeRepository;
        private readonly PriceSimulationService _priceService;
        // Tracks timestamps for logging to avoid duplicate logs within the same minute.
        private List<DateTime> _printTimes;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoringService"/> class.
        /// </summary>
        /// <param name="tradeRepository">The repository for accessing trade data.</param>
        /// <param name="priceService">The service for retrieving current prices.</param>
        public MonitoringService(TradeRepository tradeRepository, PriceSimulationService priceService)
        {
            _tradeRepository = tradeRepository;
            _priceService = priceService;
            _printTimes = new List<DateTime>();
        }

        /// <summary>
        /// Executes the monitoring logic asynchronously.
        /// </summary>
        /// <param name="stoppingToken">A token to signal when the service should stop.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var trades = await _tradeRepository.GetOpenTrades();  // Custom method to get OPEN trades
                foreach (var trade in trades)
                {
                    // Get the latest price for the trade's symbol
                    double currentPrice = _priceService.GetLatestPrice(trade.Symbol);
                    DateTime filteredDate = DateTime.UtcNow.Date.Add(new TimeSpan(DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 00));

                    // Log trade information every 5 minutes to avoid excessive logging.
                    if (filteredDate.Minute % 5 == 0 && !_printTimes.Contains(filteredDate))
                    {
                        _printTimes.Add(filteredDate);
                        PrintLog($"Trade: {trade}. Symbol Price: {currentPrice}.");
                    }

                    // Check if the Stop Loss has been hit and update the Trade's status
                    if ((trade.Side == Constants.TradeSideBuy && currentPrice <= trade.Stop_Loss)
                        || (trade.Side == Constants.TradeSideSell && currentPrice >= trade.Stop_Loss))
                    {
                        await _tradeRepository.UpdateTradeStatus(trade.ID, Constants.TradeStatusSLHit);
                        PrintLog($"Trade for {trade.Symbol} hit Stop Loss. Closed at {currentPrice}.");
                    }
                    // Check if the Target has been hit and update the Trade's status
                    else if ((trade.Side == Constants.TradeSideBuy && currentPrice >= trade.Target)
                        || (trade.Side == Constants.TradeSideSell && currentPrice <= trade.Target))
                    {
                        await _tradeRepository.UpdateTradeStatus(trade.ID, Constants.TradeStatusTargetHit);
                        PrintLog($"Trade for {trade.Symbol} hit Target. Closed at {currentPrice}.");
                    }
                }

                // Wait for 5 seconds before the next monitoring cycle.
                await Task.Delay(5000, stoppingToken);
            }
        }

        /// <summary>
        /// Prints a trace message to the console.
        /// </summary>
        private void PrintLog(string log)
        {
            Console.WriteLine($"{DateTime.UtcNow} :: MonitoringService: {log}");
        }
    }
}
