using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUCommon
{
  using Stream = System.IO.Stream;
  using MemoryStream = System.IO.MemoryStream;

  public static class IOUtils
  {
    public static async Task<byte[]> ReadStream(this Stream strm)
    {
      MemoryStream mm = new MemoryStream();
      int size = await WriteStream(strm, mm);
      
      return mm.ToArray();
    }

    public static async Task<int> WriteStream(this Stream src, Stream dest)
    {
      if (!dest.CanWrite) { throw new ArgumentException("Dest stream is not writable!"); }
      int len = 0;
      byte[] buf = new byte[4096];
      int sz = 0;

      do {
        sz = await src.ReadAsync(buf, 0,buf.Length);
        if (sz > 0) { await dest.WriteAsync(buf,0,sz); len +=sz; }
      } while(sz > 0);

      return len;
    }
  }
}
