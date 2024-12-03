using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ConfigLibrary;

namespace RTI
{

    public class WebSocketService : BackgroundService
    {
        // Dictionary to store active WebSocket connections keyed by a unique identifier
        public static ConcurrentDictionary<string, WebSocket> _clients = new ConcurrentDictionary<string, WebSocket>();
        private readonly ILogger<WebSocketService> _logger; // Logger for logging events and errors
        private readonly ConfigModel _configModel;  // Configuration model to manage app-specific configurations

        // Constructor to initialize the WebSocketService with required dependencies
        public WebSocketService(ILogger<WebSocketService> logger)
        {
            _logger = logger;
            _logger.LogInformation("Starting WebSocketService");
            _configModel = new ConfigModel("mock-connection-string", "mock-run-type", "mock-index");
        }

        // Main execution logic for the background service
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Setting up an HTTP listener to listen for WebSocket requests
            using (var listener = new HttpListener())
            {
                // Configure the listener to accept connections on a specified prefix
                listener.Prefixes.Add($"http://*:5233/{Convert.ToString(Config.Index).ToLower()}/");
                listener.Start();

                while (!stoppingToken.IsCancellationRequested)
                {
                    // Accept incoming HTTP requests
                    var context = await listener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        // Handle WebSocket connection
                        var webSocketContext = await context.AcceptWebSocketAsync(null);
                        _ = HandleWebSocketAsync(webSocketContext.WebSocket);
                    }
                    else
                    {
                        // Reject non-WebSocket requests
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
        }

        // Handles WebSocket communication with the client
        public async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var buffer = new byte[4096];    // Buffer to store incoming messages
            WebSocketReceiveResult result = null;

            try
            {
                do
                {
                    // Receive data from the WebSocket
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // Parse and process the incoming message
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var request = JsonSerializer.Deserialize<WebSocketRequest>(message, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        // Validate the request and process it
                        if (ValidateRequest(request))
                        {
                            _logger.LogInformation($"{DateTime.Now} - Successful Key - {request.Key}");
                            await ProcessRequest(request, webSocket);
                        }
                        else
                        {
                            _logger.LogInformation($"{DateTime.Now} - Bad Key: Cmd: {request.Cmd} - Type: {request.Type} - Key: {request.Key}");
                        }

                    }

                } while (!result.CloseStatus.HasValue); // Continue until the WebSocket is closed
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely || ex.WebSocketErrorCode == WebSocketError.InvalidState)
            {
                // Handle specific WebSocket errors
                _logger.LogError($"WebSocket Aborted - {DateTime.Now} - {ex.Message}");
                webSocket?.Abort();
            }
            catch (Exception ex)
            {
                // Handle general exceptions
                _logger.LogError($"Exception in HandleWebSocketAsync - {DateTime.Now} - {ex}");
            }
            finally
            {
                // Gracefully close the WebSocket connection
                if (result != null && result.CloseStatus.HasValue)
                {
                    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    _logger.LogInformation("WebSocket connection closed.");
                }
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            _logger.LogInformation($"{DateTime.Now} - Connection Closed.");
        }

        // Processes the request sent by the WebSocket client
        private async Task ProcessRequest(WebSocketRequest request, WebSocket webSocket)
        {
            if (request.Cmd == "start" && request.Type == "tick")
            {
                // Handle "start" command
                _logger.LogInformation($"{DateTime.Now} - Successful Login - {request.Key} - {request.Cmd}");
                _clients.TryAdd(request.Key, webSocket);    // Add the client to the active clients list
                await SendAck(webSocket, request);  // Send acknowledgment
            }
            else if (request.Cmd == "stop" && request.Type == "tick")
            {
                // Handle "stop" command
                _logger.LogInformation($"{DateTime.Now} - Successful Logout - {request.Key} - {request.Cmd}");
                _clients.TryRemove(request.Key, out _); // Remove the client from the active clients list
                await SendAck(webSocket, request);  // Send acknowledgment
            }
            else
            {
                // Handle invalid commands
                _logger.LogInformation($"{DateTime.Now} - Invalid Command: Cmd: {request.Cmd} - Type: {request.Type} - Key: {request.Key}");
            }
        }

        // Sends an acknowledgment response to the WebSocket client
        private async Task SendAck(WebSocket webSocket, WebSocketRequest request)
        {
            var ack = new WebSocketResponse
            {
                Cmd = "ack",
                Type = request.Type,
                Key = request.Key
            };

            var ackMessage = JsonSerializer.Serialize(ack); // Serialize the response
            var buffer = Encoding.UTF8.GetBytes(ackMessage);    // Encode the message
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            _logger.LogInformation($"{DateTime.Now.ToString()} - Ack Sent - {request.Key} - {request.Cmd}");
        }

        // Validates the request from the client by checking authentication keys
        private bool ValidateRequest(WebSocketRequest request)
        {

            // Simulate validation with hardcoded keys
            var mockAuthKeys = new Dictionary<string, string>
            {
                { "demo-key-1", "Allowed Key 1" },
                { "demo-key-2", "Allowed Key 2" },
            };

            return mockAuthKeys.ContainsKey(request.Key);

        }
    }

    public class WebSocketRequest
    {
        public string Cmd { get; set; }
        public string Type { get; set; }
        public string Key { get; set; }
    }

    public class WebSocketResponse
    {
        public string Cmd { get; set; }
        public string Type { get; set; }
        public string Key { get; set; }
    }

}
