import unittest
from flask import json
from app import app, db, Signal, Trade

class TradeExecutionTestCase(unittest.TestCase):

    def setUp(self):
        """Set up test environment before each test."""
        app.config['TESTING'] = True
        # Use an in-memory SQLite database for testing
        app.config['SQLALCHEMY_DATABASE_URI'] = 'sqlite:///:memory:'
        self.app = app.test_client()
        with app.app_context():
            db.create_all()

    def tearDown(self):
        """Clean up test environment after each test."""
        with app.app_context():
            db.session.remove()
            db.drop_all()

    def test_home_route(self):
        """Test the home route."""
        response = self.app.get('/')
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.data.decode(), 'Server is running')

    def test_receive_signal_success(self):
        """Test receiving a valid signal."""
        signal_data = {
            "symbol": "ETHUSD",
            "Full_Signal": "Buy signal for ETHUSD at current price"
        }
        response = self.app.post('/api/signal',
                                 data=json.dumps(signal_data),
                                 content_type='application/json')
        self.assertEqual(response.status_code, 200)
        response_data = json.loads(response.data)
        self.assertEqual(response_data['message'], 'Signal received')

        # Verify the signal was added to the database
        with app.app_context():
            signal = Signal.query.filter_by(Symbol="ETHUSD").first()
            self.assertIsNotNone(signal)
            self.assertEqual(signal.Symbol, "ETHUSD")
            self.assertEqual(json.loads(signal.Full_Signal), signal_data)

    def test_receive_signal_invalid_input(self):
        """Test receiving a signal with invalid input (missing symbol)."""
        signal_data = {
            "Full_Signal": "Buy signal without symbol"
        }
        response = self.app.post('/api/signal',
                                 data=json.dumps(signal_data),
                                 content_type='application/json')
        self.assertEqual(response.status_code, 400)

    def test_receive_signal_empty_payload(self):
        """Test receiving an empty payload for signal."""
        response = self.app.post('/api/signal',
                                 data=json.dumps({}),
                                 content_type='application/json')
        self.assertEqual(response.status_code, 400)
        response_data = json.loads(response.data)
        self.assertEqual(response_data['message'], 'Payload must not be empty')

    def test_get_trades_empty(self):
        """Test getting trades when no trades exist."""
        response = self.app.get('/api/trades')
        self.assertEqual(response.status_code, 200)
        self.assertEqual(json.loads(response.data), [])

    def test_get_trades_with_data(self):
        """Test getting trades when trades exist."""
        with app.app_context():
            # First, add a signal to link the trade to
            signal_data = {
                "symbol": "BTCUSD",
                "Full_Signal": "Sell signal for BTCUSD"
            }
            signal = Signal(Symbol=signal_data['symbol'], Full_Signal=json.dumps(signal_data))
            db.session.add(signal)
            db.session.commit()

            # Add a trade
            trade = Trade(
                Symbol="BTCUSD",
                Side="sell",
                Entry_Price=108590.0,
                Stop_Loss=108690.0,
                Target=108390.0,
                Status="open",
                SignalId=signal.ID
            )
            db.session.add(trade)
            db.session.commit()

        response = self.app.get('/api/trades')
        self.assertEqual(response.status_code, 200)
        trades_data = json.loads(response.data)
        self.assertEqual(len(trades_data), 1)
        self.assertEqual(trades_data[0]['Symbol'], "BTCUSD")
        self.assertEqual(trades_data[0]['Side'], "sell")
        self.assertEqual(trades_data[0]['Status'], "open")
        self.assertEqual(trades_data[0]['EntryPrice'], 108590.0)
        self.assertEqual(trades_data[0]['StopLoss'], 108690.0)
        self.assertEqual(trades_data[0]['Target'], 108390.0)
        # We won't check the exact timestamp as it's generated dynamically

    def test_api_doc(self):
        """Test the API documentation route."""
        response = self.app.get('/api/doc')
        self.assertEqual(response.status_code, 200)
        doc_data = json.loads(response.data)
        self.assertEqual(doc_data['info']['title'], 'Trade Execution API')
        self.assertEqual(doc_data['swagger'], '2.0')

if __name__ == '__main__':
    unittest.main()