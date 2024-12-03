# WebSocket Service Demo

This repository demonstrates the `WebSocketService` used in a larger proprietary project. It includes:
- A simplified WebSocket server with mock dependencies.
- A client simulator to test server functionality.

## Running the Demo
1. Clone the repository.
2. Open the project in Visual Studio.
3. Run the WebSocket server.
4. Run the client simulator to test WebSocket communication.

## Mocked Features
- **Validation**: Uses mock authentication keys instead of Azure App Configuration.
- **Endpoints**: Server listens on `ws://localhost:5233/demo/`.

## Example Output
Client sends:
```json
{"Cmd":"start", "Type":"tick", "Key":"demo-key-1"}

#### Sample JSON Response
```json
{
  "status": "success",
  "type": "tick",
  "data": {
    "symbol": "BTCUSD",
    "price": 48632.12,
    "timestamp": "2024-11-01T14:23:00Z"
  }
}
