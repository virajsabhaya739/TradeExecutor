using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeEngine
{
    /// <summary>
    /// Provides data access operations for managing trade and signal data in a SQLite database.
    /// </summary>
    public class TradeRepository
    {
        // To create database aside the project folders, we needs to go out of the bin folder
        private readonly string _connectionString = "Data Source=" + System.IO.Path.Combine("..", "..", "..", "..", "..", Constants.DataBaseName);

        /// <summary>
        /// Initializes a new instance of the <see cref="TradeRepository"/> class.
        /// Ensures the database and necessary tables are created if they don't exist.
        /// </summary>
        public TradeRepository()
        {
            InitializeDatabase();
        }

        /// <summary>
        /// Initializes the SQLite database by creating the Signal and Trade tables if they do not already exist.
        /// </summary>
        private void InitializeDatabase()
        {
            string signalTable = Constants.SignalTableName;
            string tradeTable = Constants.TradeTableName;
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS {signalTable} (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Symbol TEXT NOT NULL, 
                        Full_Signal TEXT NOT NULL,
                        Timestamp DATETIME NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS {tradeTable} (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Symbol TEXT NOT NULL,
                        Side TEXT NOT NULL,
                        Entry_Price REAL NOT NULL,
                        Stop_Loss REAL NOT NULL,
                        Target REAL NOT NULL,
                        Status TEXT NOT NULL,
                        Timestamp DATETIME NOT NULL,
                        SignalId INTEGER NOT NULL,
                        FOREIGN KEY(SignalId) REFERENCES {signalTable}(ID)
                    );";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                PrintError($"InitializeDatabase: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a new trade record to the database.
        /// </summary>
        /// <param name="trade">The trade object to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddTrade(Trade trade)
        {
            try
            {
                string tradeTable = Constants.TradeTableName;

                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = connection.CreateCommand();
                    command.CommandText = $@"
                        INSERT INTO {tradeTable} (SignalId, Symbol, Side, Entry_Price, Stop_Loss, Target, Status, Timestamp)
                        VALUES (@SignalId, @Symbol, @Side, @Entry_Price, @Stop_Loss, @Target, @Status, @Timestamp)";
                    command.Parameters.AddWithValue("@Symbol", trade.Symbol);
                    command.Parameters.AddWithValue("@Side", trade.Side);
                    command.Parameters.AddWithValue("@Entry_Price", trade.Entry_Price);
                    command.Parameters.AddWithValue("@Stop_Loss", trade.Stop_Loss);
                    command.Parameters.AddWithValue("@Target", trade.Target);
                    command.Parameters.AddWithValue("@Status", trade.Status);
                    command.Parameters.AddWithValue("@Timestamp", trade.Timestamp);
                    command.Parameters.AddWithValue("@SignalId", trade.SignalId);
                    await command.ExecuteNonQueryAsync();
                }
                PrintTrace($"AddTrade: Trade added: {trade}");
            }
            catch (Exception ex)
            {
                PrintError($"Exception in AddTrade(): " + ex.Message);
            }
        }

        /// <summary>
        /// Retrieves a list of open trades for a specific symbol from the database.
        /// </summary>
        /// <param name="symbol">The symbol to filter trades by.</param>
        /// <returns>The task result contains a list of Trade objects.</returns>
        public async Task<List<Trade>> GetTradeBySymbol(string symbol)
        {
            var trades = new List<Trade>();
            try
            {
                string tradeTable = Constants.TradeTableName;

                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = connection.CreateCommand();
                    command.CommandText = $@"SELECT * FROM {tradeTable} WHERE Symbol = @Symbol AND Status = 'OPEN'";
                    command.Parameters.AddWithValue("@Symbol", symbol);
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        trades.Add(new Trade
                        {
                            ID = reader.GetInt32(0),
                            Symbol = reader.GetString(1),
                            Side = reader.GetString(2),
                            Entry_Price = reader.GetDouble(3),
                            Stop_Loss = reader.GetDouble(4),
                            Target = reader.GetDouble(5),
                            Status = reader.GetString(6),
                            Timestamp = reader.GetDateTime(7),
                            SignalId = reader.GetInt32(8)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError($"Exception in GetTradeBySymbol({symbol}): " + ex.Message);
            }
            return trades;
        }

        /// <summary>
        /// Updates the status of a specific trade in the database.
        /// </summary>
        /// <param name="tradeId">The ID of the trade to update.</param>
        /// <param name="status">The new status for the trade.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateTradeStatus(int tradeId, string status)
        {
            try
            {
                string tradeTable = Constants.TradeTableName;

                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = connection.CreateCommand();
                    command.CommandText = $@"UPDATE {tradeTable} SET Status = @Status WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@Id", tradeId);
                    await command.ExecuteNonQueryAsync();
                }
                PrintTrace($"UpdateTradeStatus(ID: {tradeId}, Status: {status})");
            }
            catch (Exception ex)
            {
                PrintError($"Exception in UpdateTradeStatus(ID: {tradeId}, Status: {status}): " + ex.Message);
            }
        }

        /// <summary>
        /// Retrieves a list of all open trades from the database.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of Trade objects.</returns>
        public async Task<List<Trade>> GetOpenTrades()
        {
            var trades = new List<Trade>();
            try
            {
                string tradeTable = Constants.TradeTableName;

                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = connection.CreateCommand();
                    command.CommandText = $@"SELECT * FROM {tradeTable} WHERE Status = 'OPEN'";
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        trades.Add(new Trade
                        {
                            ID = reader.GetInt32(0),
                            Symbol = reader.GetString(1),
                            Side = reader.GetString(2),
                            Entry_Price = reader.GetDouble(3),
                            Stop_Loss = reader.GetDouble(4),
                            Target = reader.GetDouble(5),
                            Status = reader.GetString(6),
                            Timestamp = reader.GetDateTime(7),
                            SignalId = reader.GetInt32(8)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError("Exception in GetOpenTrades: " + ex.Message);
            }
            return trades;
        }

        #region LogHandler
        /// <summary>
        /// Prints a trace message to the console.
        /// </summary>
        private void PrintTrace(string log)
        {
            Console.WriteLine($"[Trace] {DateTime.UtcNow} :: TradeRepository: {log}");
        }

        /// <summary>
        /// Prints an error message to the console.
        /// </summary>
        private void PrintError(string err)
        {
            Console.WriteLine($"[Error] {DateTime.UtcNow} :: TradeRepository: {err}");
        }
        #endregion
    }
}
