using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMaier.SimpleDlna.Utilities;

public interface ILoggable
{
  
  public ILog InternalLogger { get; }
}

public static class LoggingExtensions
{
  public static void DebugFormat(this ILoggable self, string message, object arg0)
  {
    self.InternalLogger.DebugFormat(message, arg0);
  }

  public static void NoticeFormat(this ILoggable self, string message)
  {
    self.InternalLogger.Logger.Log(self.GetType(), Level.Notice, message, null);
  }

  public static void InfoFormat(this ILoggable self, string message)
  {
    self.InternalLogger.Logger.Log(self.GetType(),Level.Info, message, null);
  }
}
