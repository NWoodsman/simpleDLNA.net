using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NMaier.SimpleDlna.Server.Http;

public class AsyncTcpServer
{
  private readonly Socket _listener;

  public AsyncTcpServer(int port)
  {
    _listener = new Socket(AddressFamily.InterNetwork,
                           SocketType.Stream,
                           ProtocolType.Tcp);

    _listener.Bind(new IPEndPoint(IPAddress.Any, port));
    _listener.Listen(100);
  }

  public async Task StartAsync(CancellationToken token = default)
  {
    Console.WriteLine("Server listening...");

    while (!token.IsCancellationRequested)
    {
      Socket client = await _listener.AcceptAsync(token);
      Console.WriteLine("Client connected");

      _ = HandleClientAsync(client, token);
    }
  }

  private async Task HandleClientAsync(Socket client, CancellationToken token)
  {
    byte[] buffer = new byte[1024];

    try
    {
      while (!token.IsCancellationRequested)
      {
        int received = await client.ReceiveAsync(buffer, SocketFlags.None, token);

        if (received == 0)
          break; // client disconnected

        string text = Encoding.UTF8.GetString(buffer, 0, received);
        Console.WriteLine($"Received: {text}");

        string response = "Echo: " + text;
        await client.SendAsync(Encoding.UTF8.GetBytes(response), SocketFlags.None, token);
      }
    }
    catch (OperationCanceledException)
    {
      // Server shutting down
    }
    catch (SocketException ex)
    {
      Console.WriteLine($"Socket error: {ex.Message}");
    }
    finally
    {
      Console.WriteLine("Client disconnected");
      client.Close();
    }
  }
}
