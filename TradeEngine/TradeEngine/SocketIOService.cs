using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketIOClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TradeEngine
{
    /// <summary>
    /// Service for handling Socket.IO communication, specifically for receiving trade signals.
    /// </summary>
    internal class SocketIOService
    {
        private readonly SocketIOClient.SocketIO _socket;
        private readonly TradeRepository _tradeRepository;
        private readonly string _hostURL = @"http://localhost:5000";
        private readonly string TradeSignalChannel = "trade_signal";

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketIOService"/> class.
        /// </summary>
        /// <param name="tradeRepository">The repository for managing trade data.</param>
        public SocketIOService(TradeRepository tradeRepository)
        {
            _tradeRepository = tradeRepository;

            var wssURI = new Uri(_hostURL);
            var clientOptions = new SocketIOClient.SocketIOOptions()
            {
                Path = "/socket.io",
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                EIO = SocketIO.Core.EngineIO.V4
            };

            _socket = new SocketIOClient.SocketIO(wssURI, clientOptions);
            _socket.OnConnected += async (sender, e) => await HandleConnected();
            _socket.On(TradeSignalChannel, async (data) => await HandleSignal(data));
            _socket.ConnectAsync();
        }

        /// <summary>
        /// Handles the event when the Socket.IO client is connected.
        /// </summary>
        private Task HandleConnected()
        {
            PrintError("Connected to SocketIO server.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles incoming trade signals from the Socket.IO server.
        /// Parses the signal data and creates a new trade if one doesn't exist for the symbol.
        /// </summary>
        /// <param name="data">The received SocketIOResponse data.</param>
        private async Task HandleSignal(SocketIOResponse data)
        {
            try
            {
                // Parse the incoming data as a JArray and extract the signal object
                JArray jsonArray = JArray.Parse(data.ToString());
                JObject jsonObj = (JObject)jsonArray[0];

                if (jsonObj != null)
                {
                    JObject signalData = (JObject)jsonObj?["signal"]; // Extract signal object

                    if (signalData != null)
                    {
                        string symbol = signalData.Value<string>("symbol");

                        // Check if trade already exists for this signal
                        var existingTrade = await _tradeRepository.GetTradeBySymbol(symbol);
                        if (existingTrade == null || existingTrade.Count < 1)
                        {
                            // Create a new Trade object from the signal data
                            Trade trade = new Trade
                            {
                                SignalId = jsonObj.Value<int>("signal_id"),
                                Symbol = symbol,
                                Side = signalData.Value<string>("side"),
                                Entry_Price = signalData.Value<double>("entry_price"),
                                Stop_Loss = signalData.Value<double>("stop_loss"),
                                Target = signalData.Value<double>("target"),
                                Status = Constants.TradeStatusOpen,
                                Timestamp = DateTime.Now
                            };

                            // Add the new trade to the repository
                            await _tradeRepository.AddTrade(trade);

                            PrintTrace($"HandleSignal: New trade created: {trade}.");
                        }
                        else
                        {
                            PrintError($"HandleSignal: Trade for {symbol} already exists.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError($"HandleSignal: Exception in parsing and creating tarde!! - " + ex.Message);
            }
        }

        #region Loghandler
        /// <summary>
        /// Prints a trace message to the console.
        /// </summary>
        private void PrintTrace(string log)
        {
            Console.WriteLine($"[Trace] {DateTime.UtcNow} :: SocketIOService: {log}");
        }

        /// <summary>
        /// Prints an error message to the console.
        /// </summary>
        private void PrintError(string err)
        {
            Console.WriteLine($"[Error] {DateTime.UtcNow} :: SocketIOService: {err}");
        }
        #endregion
    }
}
