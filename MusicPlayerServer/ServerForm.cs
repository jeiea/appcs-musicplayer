using MusicPlayerCommon;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;

namespace MusicPlayerServer
{
  public partial class ServerForm : Form
  {
    Task Worker;
    CancellationTokenSource WorkerAbort;
    WindowsMediaPlayer Wmp = new WindowsMediaPlayer();
    AsyncManualResetEvent<bool> Dispatcher = new AsyncManualResetEvent<bool>();
    List<MyBufferBlock<byte[]>> WriteQueues = new List<MyBufferBlock<byte[]>>();

    public ServerForm()
    {
      InitializeComponent();
      AppDomain.CurrentDomain.UnhandledException += CommonBehavior.OnUnhandledException;
      TbIp.Text = CommonBehavior.GetLocalIPAddress().ToString();
      BtnBrowse.Click += (s, e) =>
      CommonBehavior.BrowseHandler(TbPath, LvTracks, s, e);
    }

    private void BtnToggleClick(object sender, EventArgs e)
    {
      if (Worker == null) StartServer();
      else StopServer();
    }

    private void StartServer()
    {
      if (!CommonBehavior.VerifyPathPort
        (TbPath, TbIp, TbPort, out IPEndPoint addr))
        return;

      TbIp.ReadOnly = TbPort.ReadOnly = TbPath.ReadOnly = true;
      WorkerAbort = new CancellationTokenSource();
      Worker = new Task(new Action(() => TcpWorker(addr)), WorkerAbort.Token);
      Worker.Start();
      BtnToggle.Text = "Stop";
    }

    void StopServer()
    {
      WorkerAbort.Cancel();
      TbIp.ReadOnly = TbPort.ReadOnly = TbPath.ReadOnly = false;
      BtnToggle.Text = "Start";
      Worker.Wait(100);
      Worker.Dispose();
      Worker = null;
      TbLog.AppendText("Server terminated.\r\n");
    }

    async void TcpWorker(IPEndPoint addr)
    {
      var listener = new TcpListener(addr);
      try
      {
        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();
        Log($"Server starts on {TbPath.Text}\r\nWaiting clients...\r\n");

        var token = WorkerAbort.Token;
        while (!token.IsCancellationRequested)
        {
          var client = await listener.AcceptTcpClientAsync();
          ClientInstance(client);
        }
      }
      catch (Exception) { }
      finally
      {
        listener.Stop();
        Log("Server terminated.\r\n");
      }
    }

    void Log(string text)
    {
      Invoke(new Action(() => TbLog.AppendText(text)));
    }

    private async void ClientInstance(TcpClient client)
    {
      string ip = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString();
      Log($"Connected from {ip}\r\n");
      try
      {
        var stream = client.GetStream();
        await PushTrackList(stream);

        var sendQueue = new MyBufferBlock<byte[]>();
        WriteQueues.Add(sendQueue);
        ClientReceiveHandler(stream, sendQueue);

        var token = WorkerAbort.Token;
        while (!token.IsCancellationRequested)
        {
          // 아마 가장 고민한 부분.
          // 그래도 고민만 할 순 없으므로, 그냥 2개로 쪼개기로.
          byte[] content = await sendQueue.ReceiveAsync(token);
          await stream.WriteAsync(content, 0, content.Length, token);
        }
      }
      catch (Exception e)
      {
        Log(e.Message);
      }
      finally
      {
        client.Close();
        Log($"Disconnected from {ip}\r\n");
      }
    }

    private async Task ClientReceiveHandler(NetworkStream reader, MyBufferBlock<byte[]> send)
    {
      while (!WorkerAbort.IsCancellationRequested)
      {
        switch (await reader.ReadLenAsync(WorkerAbort.Token))
        {
          case DownloadRequest req:
            var item = Invoke(new Func<object>(() => LvTracks.Items[req.Index].Tag)) as TrackMetadata;
            var blob = new FileBlob()
            {
              Name = Path.GetFileName(item.Path),
              Body = File.ReadAllBytes(item.Path)
            };
            send.Enqueue(AsyncUtility.GetPrefixedSerial(blob));
            break;
          case FileBlob file:
            // TODO: File upload
            break;
        }
      }
    }

    private async Task PushTrackList(NetworkStream stream)
    {
      var metas = Invoke(new Func<TrackMetadata[]>(() =>
      {
        var ar = LvTracks.Items
          .OfType<ListViewItem>().Select(x => x.Tag)
          .OfType<TrackMetadata>().ToArray();
        return ar;
      }));
      await stream.WriteLenAsync(metas);
    }
  }
}
