﻿using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
      get { return $"{(FileSize / Math.Max(Convert.ToInt32(Duration.TotalSeconds), 1) * 8 / 1024).ToString()}kbps"; }
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

    public DownloadRequest(int index) { Index = index; }
  }

  [Serializable]
  public class Announcement
  {
    public string Message;
    public Announcement(string messsage) { Message = messsage; }
  }

  public static class SerialUtility
  {
    public static BinaryFormatter Formatter = new BinaryFormatter();

    public static async Task WriteObjAsync(this Stream s, object o)
    {
      await WriteObjAsync(s, o, CancellationToken.None);
    }

    public static async Task WriteObjAsync(this Stream s, object o, CancellationToken token)
    {
      MemoryStream ms = GetPrefixedSerialStream(o);
      token.ThrowIfCancellationRequested();
      await ms.CopyToAsync(s, 8192, token);
    }

    public static async Task ThrottleCopyTo(this Stream source, Stream dest,
      int bytesPerSec, CancellationToken token)
    {
      int len = bytesPerSec / 10, read = 1;
      byte[] buf = new byte[len];
      DateTime t1 = DateTime.Now;
      while (!token.IsCancellationRequested)
      {
        read = await source.ReadAsync(buf, 0, len, token);
        if (read == 0) break;
        await dest.WriteAsync(buf, 0, read, token);
        DateTime t2 = DateTime.Now;
        await Task.Delay((int)Math.Max(100 - (t2 - t1).TotalMilliseconds, 0), token);
        t1 = t2;
      }
    }

    // async is not useful for MemoryStream: http://stackoverflow.com/a/20805616
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

    public static async Task<object> ReadObjAsync(this Stream s, Action<double> progress = null)
    {
      byte[] lenHeader = await s.ReadLenAsync(sizeof(long));

      int len = (int)BitConverter.ToInt64(lenHeader, 0);
      byte[] buf = await s.ReadLenAsync(len, progress);

      return Formatter.Deserialize(new MemoryStream(buf));
    }

    // CancellationToken does nothing for NetworkStream.ReadAsync
    public static async Task<byte[]> ReadLenAsync(this Stream s, int len, Action<double> progress = null)
    {
      if (len < 0) throw new ArgumentException("len cannnot be negative");
      byte[] buf = new byte[len];
      int read = 0, justRead;

      while (read < len && (justRead = await s.ReadAsync(buf, read, len - read)) > 0)
      {
        read += justRead;
        progress?.Invoke((double)read / len);
      }
      if (read != len) throw new EndOfStreamException();

      return buf;
    }
  }
}
