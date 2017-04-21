using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;
using MusicPlayerCommon;

namespace MusicPlayerServer
{
  public partial class ServerForm : Form
  {
    WindowsMediaPlayer Wmp = new WindowsMediaPlayer();
    CancellationTokenSource WorkerAbort;
    Task Worker;

    public ServerForm()
    {
      InitializeComponent();
      AppDomain.CurrentDomain.UnhandledException += CommonBehavior.OnUnhandledException;
      TbIp.Text = CommonBehavior.GetLocalIPAddress().ToString();
      BtnBrowse.Click += (s, e) =>
      CommonBehavior.BrowseHandler(TbPath, LvMusics, s, e);
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
      Worker.Dispose();
      TbIp.ReadOnly = TbPort.ReadOnly = TbPath.ReadOnly = false;
    }

    async void TcpWorker(IPEndPoint addr)
    {
      var listener = new TcpListener(addr);
      listener.Start();
      string success = $"Server starts on {TbPath.Text}\r\nWaiting clients...\r\n";
      Invoke(new Action(() => TbLog.AppendText(success)));

      while (!WorkerAbort.IsCancellationRequested)
      {
        try
        {
          var client = await listener.AcceptTcpClientAsync();
          var stream = client.GetStream();
          var formatter = new BinaryFormatter();
          var ms = new MemoryStream();
          var metas = Invoke(new Func<TrackMetadata[]>(() =>
          {
            var ar = LvMusics.Items
              .OfType<ListViewItem>().Select(x => x.Tag)
              .OfType<TrackMetadata>().ToArray();
            return ar;
          }));
          formatter.Serialize(ms, metas);
          var buf = ms.ToArray();
          await stream.WriteAsync(buf, 0, buf.Length);
        }
        catch (Exception) { }
      }
    }
  }
}
