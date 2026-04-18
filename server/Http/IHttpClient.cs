using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NMaier.SimpleDlna.Server.Http;

internal interface IHttpClient
{
  public void Dispose();
  public void Start();
  public void Close();
  public IPEndPoint LocalEndPoint { get; }
  public IPEndPoint RemoteEndpoint { get; }
  public IHeaders Headers { get; }
  public bool IsATimeout { get; }


  public static IHttpClient Create(HttpServer owner, TcpClient client)
  {
    throw new NotImplementedException();
  }
}
