using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using NMaier.SimpleDlna.Server.Ssdp;
using NMaier.SimpleDlna.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices.Swift;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NMaier.SimpleDlna.Server.Http;

public class AsyncTcpServer : ILoggable, IHttpServer
{
  private readonly Socket _listener;
  private ILog instance;

  private readonly ConcurrentDictionary<Guid, MediaMount> servers =
new ConcurrentDictionary<Guid, MediaMount>();

  private readonly ConcurrentDictionary<string, IPrefixHandler> prefixes =
  new ConcurrentDictionary<string, IPrefixHandler>();

  private readonly ConcurrentDictionary<Guid, List<Guid>> devicesForServers =
  new ConcurrentDictionary<Guid, List<Guid>>();

  private readonly SsdpHandler ssdpServer;

  public static readonly string Signature = GenerateServerSignature();
  public int RealPort { get; }

  public event EventHandler<HttpAuthorizationEventArgs> OnAuthorizeClient;

  public IEnumerable<(string, string)> MediaMounts => servers.Select(x => (x.Value.Prefix, x.Value.FriendlyName));


  public ILog InternalLogger
  {
    get
    {
      if (instance is null) instance = LogManager.GetLogger(GetType());
      return instance;
    }
  }



  public void Dispose()
  {
      _listener.Close();
      
  }

  public void RegisterMediaServer(IMediaServer server)
  {
    if (server == null)
    {
      throw new ArgumentNullException(nameof(server));
    }
    var guid = server.UUID;
    if (servers.ContainsKey(guid))
    {
      throw new ArgumentException("Attempting to register more than once");
    }
    
    var end = (IPEndPoint)_listener.LocalEndPoint;
    var mount = new MediaMount(server);
    servers[guid] = mount;
    RegisterHandler(mount);

    foreach (var address in IP.ExternalIPAddresses)
    {
      this.DebugFormat("Registering device for {0}", address);
      var deviceGuid = Guid.NewGuid();
      var list = devicesForServers.GetOrAdd(guid, new List<Guid>());
      lock (list)
      {
        list.Add(deviceGuid);
      }
      mount.AddDeviceGuid(deviceGuid, address);
      var uri = new Uri($"http://{address}:{end.Port}{mount.DescriptorURI}");
      lock (list)
      {
        ssdpServer.RegisterNotification(deviceGuid, uri, address);
      }
      this.NoticeFormat($"New mount at: {uri}");
    }
  }

  void RegisterHandler(IPrefixHandler handler)
  {
    if (handler == null)
    {
      throw new ArgumentNullException(nameof(handler));
    }
    var prefix = handler.Prefix;
    if (!prefix.StartsWith("/", StringComparison.Ordinal))
    {
      throw new ArgumentException("Invalid prefix; must start with /");
    }
    if (!prefix.EndsWith("/", StringComparison.Ordinal))
    {
      throw new ArgumentException("Invalid prefix; must end with /");
    }
    if (FindHandler(prefix) != null)
    {
      throw new ArgumentException("Invalid prefix; already taken");
    }
    if (!prefixes.TryAdd(prefix, handler))
    {
      throw new ArgumentException("Invalid preifx; already taken");
    }
    this.DebugFormat($"Registered Handler for {prefix}",prefix);
  }

  internal IPrefixHandler FindHandler(string prefix)
  {
    if (string.IsNullOrEmpty(prefix))
    {
      throw new ArgumentNullException(nameof(prefix));
    }

    if (prefix == "/")
    {
      return new IndexHandler(this);
    }

    return (from s in prefixes.Keys
            where prefix.StartsWith(s, StringComparison.Ordinal)
            select prefixes[s]).FirstOrDefault();
  }

  public async Task StartAsync(CancellationToken token)
  {
    Console.WriteLine("Server listening...");

    // this loop has no temporal context. It continues infinitely and only does something when called
    // which is to say awaiting the socket 
    while (!token.IsCancellationRequested)
    {
      // need to spin the client off into its own class? 

      Socket client_socket = await _listener.AcceptAsync(token);
      Console.WriteLine("Client connected");

      AsyncTcpClient client = new(client_socket, token);

      _ = client.SpinAsync();

    }
  }

  private static string GenerateServerSignature()
  {
    var os = Environment.OSVersion;
    var pstring = os.Platform.ToString();
    switch (os.Platform)
    {
      case PlatformID.Win32NT:
      case PlatformID.Win32S:
      case PlatformID.Win32Windows:
        pstring = "WIN";
        break;
      default:
        try
        {
          pstring = Formatting.GetSystemName();
        }
        catch (Exception ex)
        {
          LogManager.GetLogger(typeof(HttpServer)).Debug("Failed to get uname", ex);
        }
        break;
    }
    var version = Assembly.GetExecutingAssembly().GetName().Version;
    var bitness = IntPtr.Size * 8;
    return
      $"{pstring}{bitness}/{os.Version.Major}.{os.Version.Minor} UPnP/1.0 DLNADOC/1.5 sdlna/{version.Major}.{version.Minor}";
  }
  public AsyncTcpServer(int port)
  {
    _listener = new Socket(AddressFamily.InterNetwork,
                           SocketType.Stream,
                           ProtocolType.Tcp);

    _listener.Bind(new IPEndPoint(IPAddress.Any, port));

    prefixes.TryAdd(
       "/favicon.ico",
       new StaticHandler(
         new ResourceResponse(HttpCode.Ok, "image/icon", "favicon"))
       );
    prefixes.TryAdd(
      "/static/browse.css",
      new StaticHandler(
        new ResourceResponse(HttpCode.Ok, "text/css", "browse_css"))
      );
    RegisterHandler(new IconHandler());

    this.NoticeFormat($"Running HTTP Server: {Signature} on port {RealPort}");

    ssdpServer = new();

    RealPort = ((IPEndPoint)_listener.LocalEndPoint).Port;

    _listener.Listen(100);
  }
}
