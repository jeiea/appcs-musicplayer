using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MusicPlayerCommon
{
  [Serializable]
  public class TrackMetadata
  {
    public string Title;
    public string Artist;
    public TimeSpan Duration;
    public long FileSize;

    [NonSerialized]
    public string Path;

    public string Bitrate
    {
      get { return $"{(FileSize / Convert.ToInt32(Duration.TotalSeconds) * 8 / 1024).ToString()}kbps"; }
    }

    public string[] ListItem
    {
      get { return new string[] { "", Title, Artist, Duration.ToString("g"), Bitrate }; }
    }
  }

  [Serializable]
  public class FileBlob
  {
    public string Name;
    public byte[] Body;
  }

  [Serializable]
  public class DownloadRequest
  {
    public int Index;
  }

  public static class AsyncUtility
  {
    public static BinaryFormatter Formatter = new BinaryFormatter();

    public static async Task WriteLenAsync(this Stream s, object o)
    {
      await WriteLenAsync(s, o, CancellationToken.None);
    }

    public static async Task WriteLenAsync(this Stream s, object o, CancellationToken token)
    {
      MemoryStream ms = GetPrefixedSerialStream(o);
      token.ThrowIfCancellationRequested();
      await ms.CopyToAsync(s, 8192, token);
    }

    // async is not useful: http://stackoverflow.com/a/20805616
    static MemoryStream GetPrefixedSerialStream(object o)
    {
      var ms = new MemoryStream();
      ms.Write(BitConverter.GetBytes(0L), 0, sizeof(long));
      Formatter.Serialize(ms, o);
      ms.Seek(0, SeekOrigin.Begin);
      ms.Write(BitConverter.GetBytes(ms.Length - sizeof(long)), 0, sizeof(long));
      ms.Seek(0, SeekOrigin.Begin);
      return ms;
    }

    public static byte[] GetPrefixedSerial(object o)
    {
      MemoryStream ms = GetPrefixedSerialStream(o);
      byte[] buf = new byte[ms.Length];
      ms.Read(buf, 0, Convert.ToInt32(ms.Length));
      return buf;
    }

    public static async Task<object> ReadLenAsync(this Stream s)
    {
      return await ReadLenAsync(s, CancellationToken.None);
    }

    public static async Task<object> ReadLenAsync(this Stream s, CancellationToken token)
    {
      byte[] lenHeader = new byte[sizeof(long)];
      int read = 0, bytesRead;

      while ((bytesRead = await s.ReadAsync(lenHeader, read, sizeof(long) - read, token)) > 0
        && read < sizeof(long)) read += bytesRead;
      if (read != sizeof(long)) throw new EndOfStreamException();
      token.ThrowIfCancellationRequested();

      int len = (int)BitConverter.ToInt64(lenHeader, 0);
      byte[] buf = new byte[len];
      read = 0;
      while (read < len && (bytesRead = await s.ReadAsync(buf, read, len - read, token)) > 0)
        read += bytesRead;
      if (read != len) throw new EndOfStreamException();
      token.ThrowIfCancellationRequested();

      return Formatter.Deserialize(new MemoryStream(buf));
    }
  }
}
