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
      TbIp.Text = GetLocalIPAddress().ToString();
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

    private void BtnToggleClick(object sender, EventArgs e)
    {
      if (Worker == null) StartServer();
      else StopServer();
    }

    private void StartServer()
    {
      if (!Directory.Exists(TbPath.Text))
      {
        MessageBox.Show("유효한 MP3 파일 저장 경로가 아닙니다.");
        return;
      }
      if (!IPAddress.TryParse(TbIp.Text.Trim(), out IPAddress addr))
      {
        MessageBox.Show("유효한 IP가 아닙니다.");
        return;
      }
      if (!int.TryParse(TbPort.Text.Trim(), out int port))
      {
        MessageBox.Show("유효한 포트가 아닙니다");
        return;
      }

      TbIp.ReadOnly = TbPort.ReadOnly = TbPath.ReadOnly = true;
      WorkerAbort = new CancellationTokenSource();
      Worker = new Task(new Action(() => TcpWorker(addr, port)), WorkerAbort.Token);
      Worker.Start();
      BtnToggle.Text = "Stop";
    }

    void StopServer()
    {
      WorkerAbort.Cancel();
      Worker.Dispose();
      TbIp.ReadOnly = TbPort.ReadOnly = TbPath.ReadOnly = false;
    }

    async void TcpWorker(IPAddress addr, int port)
    {
      var listener = new TcpListener(addr, port);
      listener.Start();
      string success = $"Server starts on {TbPath.Text}\r\nWaiting clients...\r\n";
      Invoke(new Action(() => TbLog.AppendText(success)));

      while (!WorkerAbort.IsCancellationRequested)
      {
        var client = await listener.AcceptTcpClientAsync();
        var stream = client.GetStream();
        var formatter = new BinaryFormatter();
        var ms = new MemoryStream();
        var metas = LvMusics.Items
          .OfType<ListViewItem>().Select(x => x.Tag)
          .OfType<TrackMetadata>().ToArray();
        formatter.Serialize(ms, metas);
        var buf = ms.ToArray();
        await stream.WriteAsync(buf, 0, buf.Length);
      }
    }

    private void BtnBrowse_Click(object sender, EventArgs e)
    {
      FdMp3Repo.RootFolder = Environment.SpecialFolder.MyComputer;
      // TODO: 테스트 코드 제거
      //if (FdMp3Repo.ShowDialog() != DialogResult.OK) return;
      FdMp3Repo.SelectedPath = @"D:\Jeiea\Music\OSTFavorite";

      TbPath.Text = FdMp3Repo.SelectedPath;
      LvMusics.Items.Clear();

      var exts = new Regex(@"\.(mp[234a]|m4a|aac|mka|wma|wav|flac)$");
      var audios = Directory.EnumerateFiles(TbPath.Text).Where(x => exts.IsMatch(x));
      foreach (var file in audios)
      {
        var medium = Wmp.newMedia(file);
        int.TryParse(medium.getItemInfo("FileSize"), out int size);
        // getItemInfoByAtom은 구현이 안 된듯 하다.
        var meta = new TrackMetadata()
        {
          Title = medium.name,
          Artist = medium.getItemInfo("Artist"),
          Duration = new TimeSpan(Convert.ToInt64(medium.duration * 1000000000)),
          FileSize = size,
          Path = file
        };
        LvMusics.Items.Add(new ListViewItem(meta.ListItem) { Tag = meta });
      }
    }
  }
}
