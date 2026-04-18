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

    ResponseBuffer rb = new ResponseBuffer();

    try
    {
      while (!token.IsCancellationRequested)
      {
        int received_byte_count = await socket.ReceiveAsync(rb.Buffer, SocketFlags.None, token);

        if (received_byte_count == 0)
        {
          break; // client disconnected
        }

        rb.Parse();

        throw new NotImplementedException();
        //sstring response = "Echo: " + text;
        //await socket.SendAsync(Encoding.UTF8.GetBytes(response), SocketFlags.None, token);
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
      socket.Close();
    }

  }


  public AsyncTcpClient(Socket socket, CancellationToken token)
  {
    this.socket = socket;
    this.token = token;
  }
}
