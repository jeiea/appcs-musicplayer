using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;

namespace MusicPlayerCommon
{
  public static class CommonBehavior
  {
    public static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      MessageBox.Show(e.ExceptionObject.ToString());
    }

    public static IPAddress GetLocalIPAddress()
    {
      var entries = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
      var ipv6 = entries
        .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
        .FirstOrDefault() ?? IPAddress.Loopback;
      return entries.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
        .FirstOrDefault() ?? ipv6;
    }

    public static bool VerifyEndpoint(TextBox tbIp, TextBox tbPort, out IPEndPoint addr)
    {
      addr = null;
      if (!IPAddress.TryParse(tbIp.Text.Trim(), out IPAddress ip))
      {
        MessageBox.Show("유효한 IP가 아닙니다.");
        return false;
      }
      if (!int.TryParse(tbPort.Text.Trim(), out int port))
      {
        MessageBox.Show("유효한 포트가 아닙니다");
        return false;
      }
      addr = new IPEndPoint(ip, port);
      return true;
    }

    public static bool VerifyPath(TextBox tbPath)
    {
      if (Directory.Exists(tbPath.Text)) return true;
      MessageBox.Show("유효한 MP3 파일 저장 경로가 아닙니다.");
      return false;
    }

    public static void BrowseHandler(TextBox tbPath, ListView.ListViewItemCollection items)
    {
      var fd = new FolderBrowserDialog();
      fd.RootFolder = Environment.SpecialFolder.MyComputer;

      // TODO: 테스트 코드 제거
      //if (fd.ShowDialog() != DialogResult.OK) return;
      var name = System.Reflection.Assembly.GetEntryAssembly().FullName;
      fd.SelectedPath = name.StartsWith("MusicPlayerClient")
        ? @"D:\Jeiea\Study\3학년 1학기\응용소프트웨어\HW2\ClientRepo"
        : @"D:\Jeiea\Study\3학년 1학기\응용소프트웨어\HW2\ServerRepo";

      tbPath.Text = fd.SelectedPath;
      items.Clear();
      var wmp = new WindowsMediaPlayer();

      var exts = new Regex(@"\.(mp[234a]|m4a|aac|mka|wma|wav|flac)$");
      var audios = Directory.EnumerateFiles(tbPath.Text).Where(x => exts.IsMatch(x));
      foreach (var file in audios)
      {
        TrackMetadata meta = GetMetadata(wmp, file);
        items.Add(new ListViewItem(meta.ListItem) { Tag = meta });
      }
    }

    public static TrackMetadata GetMetadata(WindowsMediaPlayer wmp, string file)
    {
      var medium = wmp.newMedia(file);
      long.TryParse(medium.getItemInfo("FileSize"), out long size);
      // getItemInfoByAtom은 구현이 안 된듯 하다.
      var meta = new TrackMetadata()
      {
        Title = medium.name,
        Artist = medium.getItemInfo("Artist"),
        Duration = new TimeSpan(Convert.ToInt64(medium.duration * 10000000)),
        FileSize = size,
        Path = file
      };
      return meta;
    }
  }

  public class MyDispatcher
  {
    SynchronizationContext Context;

    public MyDispatcher()
    {
      Context = SynchronizationContext.Current;
    }

    public void Post(Action action)
    {
      Context.Post(new SendOrPostCallback(o => action()), null);
    }

    public Task<T> Send<T>(Func<T> expr)
    {
      T holder = default(T);
      return Task.Run(() =>
      {
        Context.Send(new SendOrPostCallback(_ => { holder = expr(); }), null);
        return holder;
      });
    }
  }

  public static class AsyncUtility
  {
    public static void IgnoreExceptions(this Task task)
    {
      task.ContinueWith(c => { var ignored = c.Exception; },
        TaskContinuationOptions.OnlyOnFaulted |
        TaskContinuationOptions.ExecuteSynchronously);
    }

    public static Task GetHold(this CancellationToken token)
    {
      var task = new TaskCompletionSource<bool>();
      token.Register(() => task.SetResult(true));
      return task.Task;
    }
  }

  public class MyBufferBlock<T>
  {
    private ConcurrentQueue<T> DataQueue = new ConcurrentQueue<T>();
    private ConcurrentQueue<TaskCompletionSource<T>> Workers =
      new ConcurrentQueue<TaskCompletionSource<T>>();

    public Task<T> ReceiveAsync()
    {
      return ReceiveAsync(CancellationToken.None);
    }

    public Task<T> ReceiveAsync(CancellationToken token)
    {
      var ret = new TaskCompletionSource<T>();
      if (DataQueue.TryDequeue(out T res))
      {
        ret.SetResult(res);
      }
      else
      {
        Workers.Enqueue(ret);
        token.Register(new Action(() => ret.SetException(new TaskCanceledException())));
      }
      return ret.Task;
    }

    public void Enqueue(T value)
    {
      while (Workers.TryDequeue(out TaskCompletionSource<T> worker))
      {
        if (worker.TrySetResult(value)) return;
      }
      DataQueue.Enqueue(value);
    }

    public void Abort()
    {
      while (Workers.TryDequeue(out TaskCompletionSource<T> worker))
      {
        worker.SetException(new TaskCanceledException());
      }
    }
  }
  public class AsyncManualResetEvent<T>
  {
    private volatile TaskCompletionSource<T> m_tcs = new TaskCompletionSource<T>();

    public Task<T> WaitAsync() { return m_tcs.Task; }

    public void Reset()
    {
      var new_tcs = new TaskCompletionSource<T>();
      while (true)
      {
        var tcs = m_tcs;
        if (!tcs.Task.IsCompleted || Interlocked.CompareExchange(ref m_tcs, new_tcs, tcs) == tcs)
          return;
      }
    }

    public void Abort()
    {
      var tcs = m_tcs;
      Task.Factory.StartNew(s => ((TaskCompletionSource<T>)s).TrySetException(new TaskCanceledException()),
          tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
      tcs.Task.Wait();
    }

    public void Set(T value)
    {
      var tcs = m_tcs;
      Task.Factory.StartNew(s => ((TaskCompletionSource<T>)s).TrySetResult(value),
          tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
      tcs.Task.Wait();
    }
  }
}
