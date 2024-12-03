using System.Net.WebSockets;
using System.Text;

public class WebSocketClientSimulator
{
    public static async Task Main(string[] args)
    {
        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri("ws://localhost:5233/demo/"), CancellationToken.None);

        // Send a test request
        var request = "{\"Cmd\":\"start\", \"Type\":\"tick\", \"Key\":\"demo-key-1\"}";
        var buffer = Encoding.UTF8.GetBytes(request);
        await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

        // Wait for response
        var responseBuffer = new byte[4096];
        var result = await client.ReceiveAsync(new ArraySegment<byte>(responseBuffer), CancellationToken.None);
        var response = Encoding.UTF8.GetString(responseBuffer, 0, result.Count);

        Console.WriteLine($"Received: {response}");
    }
}
