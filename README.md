# TradeExecutor

## Overview
This project simulates a trading system with .NET and Flask apps.

## Project Structure
- `TradeEngine/`: Contains the .NET Core application for handling trading logic, price simulation, and monitoring.
- `ServerApp/`: Contains the Flask application for the interacting with the .NET backend.
- `TradeExecution.db`: The shared SQLite database file.
- `Dockerfile_Engine`: Dockerfile for containerizing the .NET application.
- `Dockerfile_Server`: Dockerfile for containerizing the Flask application.

## Setup
1. Clone the repository.
2. For .NET: Run `dotnet restore` and `dotnet run` in the .NET folder.
3. For Flask: Create a virtual env, install requirements, and run `python app.py`.
4. Ensure both apps can access the SQLite database.

## Usage
- Send signals via POST /api/signal.
- View trades via GET /api/trades.

## Bonus Features
- Unit tests in .NET.
- Docker: Build and run using the provided Dockerfiles.