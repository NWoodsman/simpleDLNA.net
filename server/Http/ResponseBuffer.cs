using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMaier.SimpleDlna.Server.Http;

enum BufferStatus
{
  Empty,
  FilledButUnparsed,
  FilledAndParsed
}

enum Methods
{
  GET,
  POST,
  PUT,
  DELETE,
  HEAD,
  PATCH,
  OPTIONS,
  CONNECT,
  TRACE
}




public class ResponseBuffer
{
 static UTF8Encoding decoder = new UTF8Encoding();

  List<char> parsedChars = new();
  byte[] buffer = new byte[1024];
  char[] char_buffer = new char[1024];
  int current_char_buffer_length = 0;

  int current_status_code = 0;
  string path = string.Empty;


  internal byte[] Buffer => buffer;

  internal void Parse()
  {
    current_char_buffer_length = decoder.GetChars(buffer,char_buffer);

    switch (current_char_buffer_length)
    {
      case int i when i > 3:
        Span<char> maybeStatus = char_buffer.AsSpan()[0..3];
        switch (maybeStatus[0], maybeStatus[1], maybeStatus[2])
        {
          // assume we have a full http request incoming
          case ('G', 'E', 'T'):
            int slen = ReadToEndOfStatus();
            ReadToEndOfHeader(Methods.GET,slen);
            break;
          case var x:
            break;
          default:
            break;
        }

        break;

      case 0:
        // assume we flush the buffer 
        break;
    }

    
  }

  int methodLength(Methods method) => method switch
  {
    Methods.GET =>3,
    Methods.CONNECT =>7,
    Methods.DELETE =>6,
    Methods.PUT =>3,
    Methods.POST =>4,
    Methods.HEAD =>4,
    Methods.OPTIONS =>7,
    Methods.PATCH => 5,
    Methods.TRACE => 5,
    _=> throw new ArgumentOutOfRangeException()
  };


  void ReadToEndOfHeader(Methods context, int endOfStatusPos)
  {
      int startpos = methodLength(context)+endOfStatusPos+1;

    if (char_buffer[startpos] != ' ') throw new InvalidDataException("Malformed http header. expected a space before the first header");

    startpos++;

   var balanceSpan = char_buffer.AsSpan().Slice(startpos);

    // this means the status ended with \r\n so we had no headers after the status
    if (balanceSpan.Length == 2)
    {
      return;
    }

    int pos = 0;

    while (pos < balanceSpan.Length)
    {
      while (balanceSpan[pos] != '\n') pos++;

      var next_header = balanceSpan.Slice(0, pos);


    }





  }

  int ReadToEndOfStatus()
  {
    int status_length = 0;

    if (char_buffer[3] != ' ') throw new InvalidDataException("Malformatted http request. Expected a space after GET");

    // start at 4 assuming 3 digit http code + 1 space char before the rest of the status
    for (int i = 4; i < current_char_buffer_length; i++)
    {
      if (char_buffer[i] != '\n') status_length++;
      else break;
    }

    if (char_buffer[3 + status_length] == '\r')
    {
      --status_length;
    }

    Span<char> statusBuf = char_buffer.AsSpan().Slice(4, status_length);

    var split_index = statusBuf.IndexOf(' ');

    var firstChunk = statusBuf.Slice(0, split_index);
    var secondChunk = statusBuf.Slice(split_index, status_length - split_index);

    current_status_code = int.Parse(firstChunk);
    path = secondChunk.ToString();

    return status_length;

  }


}
