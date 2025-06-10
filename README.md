# TradeExecutor

## Overview
This project implements a simulated trading system with a .NET Core backend for trade execution and monitoring, and a Flask application acting as a simple gateway for receiving trading signals and viewing trade status. The core logic, including simulating market price fluctuations, executing trades with Stop Loss and Target Price, and monitoring open trades to automatically close them, resides within the .NET application.

## Project Structure
- `TradeEngine/`: Contains the .NET Core application responsible for:
    - Implementing the core trading logic.
    - Receiving trading signals via a REST API.
    - Simulating trade execution and storing trades in a SQLite database.
    - Simulating a market price feed with random fluctuations.
    - Monitoring open trades and automatically closing them when Stop Loss or Target Price is hit.
    - Providing an API endpoint to retrieve trade status.
- `ServerApp/`: Contains a simple Flask application that acts as a proxy or gateway. It receives signals and trade requests and forwards them to the appropriate endpoints on the running .NET backend. It does **not** include a user interface.
- `TradeExecution.db`: The shared SQLite database file used by the .NET application to store trade data.
- `Dockerfile_Engine.dockerfile`: Dockerfile for containerizing the .NET application.
- `Dockerfile_Server.dockerfile`: Dockerfile for containerizing the Flask application.
- `requirements.txt`: Lists the Python dependencies for the Flask application.
- `Trade Signal Execution.postman_collection.json`: A Postman collection for testing the API endpoints.
- Testcases are also included. For TradeEngine check `TradeEngine/TradeEngine.Tests` project and for ServerApp check `ServerApp/test_app.py` file

## Setup

**Prerequisites:**
- .NET SDK 6.0 or later
- Python 3.9 or later
- Docker (optional, for containerized deployment)

**Local Setup:**

1.  **Clone the repository:**
    ```bash
    git clone git@github.com:virajsabhaya739/TradeExecutor.git
    cd TradeExecutor
    ```

2.  **Setup the .NET Trade Engine:**
    ```bash
    cd TradeEngine
    dotnet restore
    dotnet build
    # To run locally:
    # dotnet run
    ```

3.  **Setup the Flask Server App:**
    ```bash
    cd ../ServerApp
    python -m venv venv
    source venv/bin/activate  # On Windows use `venv\Scripts\activate`
    pip install -r requirements.txt
    # To run locally:
    # python app.py
    ```

## Usage

Assuming both the .NET Trade Engine and Flask Server App are running locally:

-   **Send a trading signal:**
    Send a `POST` request to `/api/signal` on the Flask server's address and port (e.g., `http://localhost:5000/api/signal`). The Flask app then forwards this signal to the .NET Trade Engine. The request body should be a JSON object with the following structure:
    ```json
    {
      "symbol": "BTCUSD",
      "side": "BUY",
      "entry_price": 68000,
      "stop_loss": 67500,
      "target": 69000
    }
    ```

-   **View trade status:**
    Send a `GET` request to `/api/trades` on the Flask server's address and port (e.g., `http://localhost:5000/api/trades`). The Flask app then retrieves the trade status from the shared database and returns it.
