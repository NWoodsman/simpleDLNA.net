using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NMaier.SimpleDlna.Server.Http;
public class AsyncTcpClient
{
  // the connection 
  Socket socket;

  // the token to hold while this client operates.
  CancellationToken token;

  // Keeps the client spinning looking for communications
  // When it detects 
  public async Task SpinAsync()
  {
    byte[] buffer = new byte[1024];

    try
    {
      while (!token.IsCancellationRequested)
      {
        int received_byte_count = await socket.ReceiveAsync(buffer, SocketFlags.None, token);

        if (received_byte_count == 0)
        {
          break; // client disconnected
        }

        string text = Encoding.UTF8.GetString(buffer, 0, received_byte_count);
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


  public AsyncTcpClient(Socket socket, CancellationToken token)
  {
    this.socket = socket;
    this.token = token;
  }
}
