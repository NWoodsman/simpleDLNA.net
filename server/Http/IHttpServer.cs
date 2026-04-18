using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMaier.SimpleDlna.Server.Http;

public interface IHttpServer
{
  public IEnumerable<(string, string)> MediaMounts { get; }

  public event EventHandler<HttpAuthorizationEventArgs> OnAuthorizeClient;

}
