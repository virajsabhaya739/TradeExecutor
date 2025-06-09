using Microsoft.Data.Sqlite;

namespace TradeEngine.Tests
{
    /// <summary>
    /// Unit tests for the TradeRepository class using an in-memory SQLite database.
    /// </summary>
    [TestFixture]
    public class TradeRepositoryTests : IDisposable
    {
        private class Signal
        {
            public int ID { get; set; }
            public string Symbol { get; set; }
            public string Full_Signal { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private readonly SqliteConnection _connection;
        private readonly TradeRepository _tradeRepository;
        private readonly static string _dbFileName = "TradeExecution_Test.db";
        private readonly string _connectionString = "DataSource=" + _dbFileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="TradeRepositoryTests"/> class.
        /// Sets up an in-memory SQLite database and initializes the TradeRepository.
        /// </summary>
        public TradeRepositoryTests()
        {
            if (System.IO.File.Exists(_dbFileName))
            {
                System.IO.File.Delete(_dbFileName);
            }

            // Create and open an in-memory SQLite database connection
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();

            // Initialize the database schema for testing
            InitializeDatabase(_connection);

            // Initialize the TradeRepository with the in-memory connection string
            _tradeRepository = new TradeRepository(_connectionString);
        }

        /// <summary>
        /// Initializes the database schema for testing.
        /// </summary>
        /// <param name="connection">The SQLite connection to use.</param>
        private void InitializeDatabase(SqliteConnection connection)
        {
            string signalTable = Constants.SignalTableName;
            string tradeTable = Constants.TradeTableName;
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


        /// <summary>
        /// Disposes of the in-memory database connection.
        /// </summary>
        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }

        /// <summary>
        /// Adds a signal record to the database.
        /// </summary>
        /// <param name="signal">The signal to add.</param>
        /// <returns>The ID of the newly added signal.</returns>
        private async Task<int> AddSignalAsync(Signal signal)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = $@"
                    INSERT INTO {Constants.SignalTableName} (Symbol, Full_Signal, Timestamp)
                    VALUES (@Symbol, @FullSignal, @Timestamp);
                    SELECT last_insert_rowid();"; // Get the ID of the inserted row
                command.Parameters.AddWithValue("@Symbol", signal.Symbol);
                command.Parameters.AddWithValue("@FullSignal", signal.Full_Signal);
                command.Parameters.AddWithValue("@Timestamp", signal.Timestamp);
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
        }

        private async Task TruncateTables()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = $@"DELETE FROM {Constants.TradeTableName};
                    DELETE FROM {Constants.SignalTableName};";
                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Tests that adding a trade successfully inserts it into the database.
        /// </summary>
        [Test]
        public async Task AddTrade_ShouldInsertTradeIntoDatabase()
        {
            // Arrange
            // Add a corresponding signal first
            var signal = new Signal
            {
                Symbol = "BTCUSD",
                Full_Signal = "Buy signal for BTCUSD",
                Timestamp = DateTime.UtcNow
            };
            var signalId = await AddSignalAsync(signal);

            var trade = new Trade
            {
                SignalId = signalId,
                Symbol = "BTCUSD",
                Side = Constants.TradeSideBuy,
                Entry_Price = 50000,
                Stop_Loss = 49000,
                Target = 55000,
                Status = Constants.TradeStatusOpen,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _tradeRepository.AddTrade(trade);

            // Assert
            // Retrieve the trade from the database and verify it was added
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM {Constants.TradeTableName}";
                var count = (long)await command.ExecuteScalarAsync();
                Assert.That(count, Is.EqualTo(1));

                command.CommandText = $"SELECT * FROM {Constants.TradeTableName} WHERE Symbol = @Symbol";
                command.Parameters.AddWithValue("@Symbol", trade.Symbol);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    Assert.That(await reader.ReadAsync(), Is.True); // Ensure a row was returned
                    Assert.That(reader.GetString(1), Is.EqualTo(trade.Symbol));
                    Assert.That(reader.GetString(2), Is.EqualTo(trade.Side));
                    Assert.That(reader.GetDouble(3), Is.EqualTo(trade.Entry_Price));
                    Assert.That(reader.GetDouble(4), Is.EqualTo(trade.Stop_Loss));
                    Assert.That(reader.GetDouble(5), Is.EqualTo(trade.Target));
                    Assert.That(reader.GetString(6), Is.EqualTo(trade.Status));
                    // Compare formatted strings for DateTime due to potential precision differences
                    Assert.That(reader.GetDateTime(7).ToString("yyyy-MM-dd HH:mm:ss"), Is.EqualTo(trade.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")));
                    Assert.That(reader.GetInt32(8), Is.EqualTo(trade.SignalId));
                }
            }

            await TruncateTables();
        }

        /// <summary>
        /// Tests that retrieving trades by symbol returns only open trades for that symbol.
        /// </summary>
        [Test]
        public async Task GetTradeBySymbol_ShouldReturn()
        {
            // Arrange
            // Add corresponding signals first
            var signal1 = new Signal { Symbol = "ETHUSD", Full_Signal = "Signal 1", Timestamp = DateTime.UtcNow };
            var signal2 = new Signal { Symbol = "ETHUSD", Full_Signal = "Signal 2", Timestamp = DateTime.UtcNow };
            var signal3 = new Signal { Symbol = "BTCUSD", Full_Signal = "Signal 3", Timestamp = DateTime.UtcNow };

            var signalId1 = await AddSignalAsync(signal1);
            var signalId2 = await AddSignalAsync(signal2);
            var signalId3 = await AddSignalAsync(signal3);

            var trade1 = new Trade
            {
                SignalId = signalId1,
                Symbol = "ETHUSD",
                Side = Constants.TradeSideBuy,
                Entry_Price = 3000,
                Stop_Loss = 2900,
                Target = 3500,
                Status = Constants.TradeStatusOpen,
                Timestamp = DateTime.UtcNow
            };
            var trade2 = new Trade
            {
                SignalId = signalId2,
                Symbol = "ETHUSD",
                Side = Constants.TradeSideSell,
                Entry_Price = 3100,
                Stop_Loss = 3200,
                Target = 2800,
                Status = Constants.TradeStatusClosed, // This one should not be returned
                Timestamp = DateTime.UtcNow
            };
            var trade3 = new Trade
            {
                SignalId = signalId3,
                Symbol = "BTCUSD",
                Side = Constants.TradeSideBuy,
                Entry_Price = 51000,
                Stop_Loss = 50000,
                Target = 56000,
                Status = Constants.TradeStatusOpen,
                Timestamp = DateTime.UtcNow
            };

            await _tradeRepository.AddTrade(trade1);
            await _tradeRepository.AddTrade(trade2);
            await _tradeRepository.AddTrade(trade3);

            // Act
            var openTrades = await _tradeRepository.GetTradeBySymbol("ETHUSD");

            // Assert
            Assert.That(openTrades, Is.Not.Null);
            Assert.That(openTrades.Count, Is.EqualTo(1)); // Only trade1 should be returned
            var returnedTrade = openTrades.First();
            Assert.That(returnedTrade.Symbol, Is.EqualTo(trade1.Symbol));
            Assert.That(returnedTrade.Status, Is.EqualTo(trade1.Status));

            await TruncateTables();
        }

        /// <summary>
        /// Tests that updating a trade's status successfully updates it in the database.
        /// </summary>
        [Test]
        public async Task UpdateTradeStatus_ShouldUpdateStatusInDatabase()
        {
            // Arrange
            // Add a corresponding signal first
            var signal = new Signal
            {
                Symbol = "LTCUSD",
                Full_Signal = "Signal for LTCUSD",
                Timestamp = DateTime.UtcNow
            };
            var signalId = await AddSignalAsync(signal);

            var trade = new Trade
            {
                SignalId = signalId,
                Symbol = "LTCUSD",
                Side = Constants.TradeSideBuy,
                Entry_Price = 100,
                Stop_Loss = 95,
                Target = 120,
                Status = Constants.TradeStatusOpen,
                Timestamp = DateTime.UtcNow
            };
            await _tradeRepository.AddTrade(trade);

            // Retrieve the added trade to get its ID
            List<Trade> addedTrades;
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {Constants.TradeTableName} WHERE Symbol = @Symbol";
                command.Parameters.AddWithValue("@Symbol", trade.Symbol);
                using var reader = await command.ExecuteReaderAsync();
                addedTrades = new List<Trade>();
                while (await reader.ReadAsync())
                {
                    addedTrades.Add(new Trade
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
            Assert.That(addedTrades.Count, Is.EqualTo(1));
            var tradeIdToUpdate = addedTrades.First().ID;
            var newStatus = Constants.TradeStatusTargetHit;

            // Act
            await _tradeRepository.UpdateTradeStatus(tradeIdToUpdate, newStatus);

            // Assert
            // Retrieve the trade again and verify the status is updated
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT Status FROM {Constants.TradeTableName} WHERE ID = @Id";
                command.Parameters.AddWithValue("@Id", tradeIdToUpdate);
                var updatedStatus = (string)await command.ExecuteScalarAsync();
                Assert.That(updatedStatus, Is.EqualTo(newStatus));
            }

            await TruncateTables();
        }

        /// <summary>
        /// Tests that retrieving open trades returns all trades with the "Open" status.
        /// </summary>
        [Test]
        public async Task GetOpenTrades_ShouldReturnAllOpenTrades()
        {
            // Arrange
            // Add corresponding signals first
            var signal1 = new Signal { Symbol = "XRPUSD", Full_Signal = "Signal 1", Timestamp = DateTime.UtcNow };
            var signal2 = new Signal { Symbol = "ADAUSD", Full_Signal = "Signal 2", Timestamp = DateTime.UtcNow };
            var signal3 = new Signal { Symbol = "SOLUSD", Full_Signal = "Signal 3", Timestamp = DateTime.UtcNow };

            var signalId1 = await AddSignalAsync(signal1);
            var signalId2 = await AddSignalAsync(signal2);
            var signalId3 = await AddSignalAsync(signal3);

            var trade1 = new Trade
            {
                SignalId = signalId1,
                Symbol = "XRPUSD",
                Side = Constants.TradeSideBuy,
                Entry_Price = 0.5,
                Stop_Loss = 0.48,
                Target = 0.6,
                Status = Constants.TradeStatusOpen,
                Timestamp = DateTime.UtcNow
            };
            var trade2 = new Trade
            {
                SignalId = signalId2,
                Symbol = "ADAUSD",
                Side = Constants.TradeSideSell,
                Entry_Price = 0.3,
                Stop_Loss = 0.32,
                Target = 0.28,
                Status = Constants.TradeStatusOpen,
                Timestamp = DateTime.UtcNow
            };
            var trade3 = new Trade
            {
                SignalId = signalId3,
                Symbol = "SOLUSD",
                Side = Constants.TradeSideBuy,
                Entry_Price = 150,
                Stop_Loss = 140,
                Target = 180,
                Status = Constants.TradeStatusClosed, // This one should not be returned
                Timestamp = DateTime.UtcNow
            };

            await _tradeRepository.AddTrade(trade1);
            await _tradeRepository.AddTrade(trade2);
            await _tradeRepository.AddTrade(trade3);

            // Act
            var openTrades = await _tradeRepository.GetOpenTrades();

            // Assert
            Assert.That(openTrades, Is.Not.Null);
            Assert.That(openTrades.Count, Is.EqualTo(2)); // Should return trade1 and trade2
            Assert.That(openTrades.Any(t => t.Symbol == "XRPUSD" && t.Status == Constants.TradeStatusOpen), Is.True);
            Assert.That(openTrades.Any(t => t.Symbol == "ADAUSD" && t.Status == Constants.TradeStatusOpen), Is.True);
            Assert.That(openTrades.Any(t => t.Symbol == "SOLUSD"), Is.False);

            await TruncateTables();
        }
    }
}
