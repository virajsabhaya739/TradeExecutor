from flask import Flask, request, jsonify
from flask_socketio import SocketIO
from flask_swagger import swagger
from flask_sqlalchemy import SQLAlchemy
import os, json

app = Flask(__name__)
app.config['SECRET_KEY'] = '24d1ed883c8ade6e499ad372dd0213c41e9efc3d06e2c424947929ba6223bd55'
db_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'TradeExecution.db'))
app.config['SQLALCHEMY_DATABASE_URI'] = f'sqlite:///{db_path}'
app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False

swagger_template = {
    "swagger": "2.0",
    "info": {
        "title": "Trade Execution API",
        "version": "1.0.0",
        "description": "API for handling trading Signal and Trade"
    }
}

db = SQLAlchemy(app)
socketio = SocketIO(app)

class Signal(db.Model):
    __tablename__ = "Signal"

    ID = db.Column(db.Integer, primary_key=True, autoincrement=True)
    Symbol = db.Column(db.Text, nullable=False)
    Full_Signal = db.Column(db.Text, nullable=False)
    Timestamp = db.Column(db.DateTime, nullable=False, default=db.func.now())

    def __repr__(self):
        return f'<Signal({self.ID}): {self.Full_Signal}'

class Trade(db.Model):
    __tablename__ = "Trade"

    ID = db.Column(db.Integer, primary_key=True, autoincrement=True)
    Symbol = db.Column(db.Text, nullable=False)
    Side = db.Column(db.Text, nullable=False)
    Entry_Price = db.Column(db.Float, nullable=False)
    Stop_Loss = db.Column(db.Float, nullable=False)
    Target = db.Column(db.Float, nullable=False)
    Status = db.Column(db.Text, nullable=False)
    Timestamp = db.Column(db.DateTime, nullable=False, default=db.func.now())
    SignalId = db.Column(db.Integer, db.ForeignKey('Signal.ID'), nullable=False)

    def __repr__(self):
            return f'<TradeID({self.ID}): {self.Symbol}, {self.Side}, {self.Status}'


with app.app_context():
    db.create_all()  # Create tables if they don't exist

@app.route('/', methods=['GET'])
def home():
    return 'Server is running'

# Route: POST /api/signal
@app.route('/api/signal', methods=['POST'])
def receive_signal():
    """
    Receive a trading signal
    ---
    tags:
      - signal
    parameters:
      - in: body
        name: body
        description: JSON object containing the signal data
        required: true
        schema:
          type: object
          required:
            - symbol
          properties:
            symbol:
              type: string
              description: The trading symbol (e.g., 'AAPL')
            Full_Signal:
              type: string
              description: The full signal details
    responses:
      200:
        description: Signal received and emitted successfully
        schema:
          type: object
          properties:
            message:
              type: string
              description: Success message
      400:
        description: Invalid input
    """
    data = request.json

    if data is not None and 'symbol' in data:
        signal = Signal(
            Symbol=data['symbol'], 
            Full_Signal=json.dumps(data)
        )
        db.session.add(signal)
        db.session.commit()

        # Emit to SocketIO clients
        socketio.emit('trade_signal', {'signal_id': signal.ID, 'signal_symbol': signal.Symbol, 'signal': data})
        
        return jsonify({"message": "Signal received"}), 200
    else:
        return jsonify({"message": "Payload must not be empty"}), 400

@app.route('/api/trades', methods=['GET'])
def get_trades():
    """
    Get all trades
    ---
    tags:
      - trades
    parameters: []  # No parameters needed for this GET request
    responses:
      200:
        description: List of trades
        schema:
          type: array
          items:
            type: object
            properties:
              ID:
                type: integer
                description: Trade ID
              Symbol:
                type: string
                description: Trading symbol
              Side:
                type: string
                description: Trade side (e.g., 'buy' or 'sell')
              Status:
                type: string
                description: Trade status (e.g., 'open' or 'closed')
              EntryPrice:
                type: number
                description: Entry price
              StopLoss:
                type: number
                description: Stop loss price
              Target:
                type: number
                description: Target price
              Timestamp:
                type: string
                format: date-time
                description: Timestamp of the trade
    """
    trades = Trade.query.all()
    return jsonify([{
        'ID': trade.ID,
        'Symbol': trade.Symbol,
        'Side': trade.Side,
        'Status': trade.Status,
        'EntryPrice': trade.Entry_Price,
        'StopLoss': trade.Stop_Loss,
        'Target': trade.Target,
        'Timestamp': trade.Timestamp
    } for trade in trades])

# Swagger documentation
@app.route('/api/doc', methods=['GET'])
def api_doc():
    return jsonify(swagger(app, template=swagger_template))

if __name__ == '__main__':
    socketio.run(app, debug=True, host='0.0.0.0', port=5000)