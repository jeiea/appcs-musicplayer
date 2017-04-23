using MusicPlayerCommon;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;

namespace MusicPlayerClient
{
  public partial class ClientForm : Form
  {
    Task Worker;
    CancellationTokenSource WorkerAbort;
    WindowsMediaPlayer Wmp = new WindowsMediaPlayer();
    List<IWMPMedia> Media = new List<IWMPMedia>();
    List<int> LastSelectedIndices = new List<int>();
    System.Windows.Forms.Timer TrackBarTimer =
      new System.Windows.Forms.Timer() { Interval = 1000 };

    public ClientForm()
    {
      InitializeComponent();
      AppDomain.CurrentDomain.UnhandledException += CommonBehavior.OnUnhandledException;
      BtnBrowse.Click += (s, e) =>
      CommonBehavior.BrowseHandler(TbPath, LvLocalList, s, e);
      TbIp.Text = CommonBehavior.GetLocalIPAddress().ToString();
      Wmp.currentPlaylist = Wmp.newPlaylist("no title", "");
      Wmp.MediaChange += OnWmpMediaChange;
      TrackBarTimer.Tick += TrackBarUpdateTick;
    }

    private void TrackBarUpdateTick(object sender, EventArgs e)
    {
      TrProgress.Value = Convert.ToInt32(Wmp.controls.currentPosition);
    }

    private void OnWmpMediaChange(object obj)
    {
      if (!(obj is IWMPMedia media)) return;
      LbPlayerStatus.Text = media.name;
      TrProgress.Maximum = Convert.ToInt32(media.duration);
    }

    private void BtnToggleClick(object sender, EventArgs e)
    {
      if (Worker != null)
      {
        WorkerAbort.Cancel();
        Worker.Wait(100);
        ReflectDisconnection();
        return;
      }
      if (!CommonBehavior.VerifyPathPort(TbPath, TbIp, TbPort, out IPEndPoint addr))
        return;

      WorkerAbort = new CancellationTokenSource();
      Worker = new Task(new Action(() => TcpWorker(addr)), WorkerAbort.Token);
      Worker.Start();
      BtnToggle.Text = "Connecting...";
    }

    void ReflectConnection()
    {
      BtnToggle.Text = "Disconnect";
      BtnToggle.ForeColor = Color.Red;
      TbIp.ReadOnly = TbPort.ReadOnly = TbPath.ReadOnly = true;
    }

    void ReflectDisconnection()
    {
      Worker = null;
      BtnToggle.Text = "Connect";
      BtnToggle.ForeColor = Color.Black;
      TbIp.ReadOnly = TbPort.ReadOnly = TbPath.ReadOnly = false;
      lvRemoteList.Items.Clear();
    }

    async void TcpWorker(IPEndPoint addr)
    {
      var client = new TcpClient();
      try
      {
        await client.ConnectAsync(addr.Address, addr.Port);

        Invoke(new Action(ReflectConnection));
        var stream = client.GetStream();
        WorkerAbort.Token.Register(() => { stream.Close(); });
        while (!WorkerAbort.IsCancellationRequested)
        {
          var metas = await stream.ReadObjAsync() as TrackMetadata[];
          var items = metas.Select(x => new ListViewItem(x.ListItem)).ToArray();
          Invoke(new Action(() =>
          {
            lvRemoteList.Items.Clear();
            lvRemoteList.Items.AddRange(items);
          }));
        }
      }
      catch (Exception e) {
        MessageBox.Show(e.Message);
      }
      finally
      {
        client.Close();
        Invoke(new Action(ReflectDisconnection));
      }
    }

    private void PlayButton_Click(object sender, EventArgs e)
    {
      if (LvLocalList.Items.Count <= 0) return;
      if (Wmp.playState == WMPPlayState.wmppsPlaying)
      {
        Wmp.controls.pause();
        PlayButton.Image = Properties.Resources.Play;
        TrackBarTimer.Enabled = false;
        return;
      }

      if (LastSelectedIndices.SequenceEqual(LvLocalList.SelectedIndices.OfType<int>()) &&
          LastSelectedIndices.Count != 0)
      {
        WmpPlay();
        return;
      }

      Wmp.currentPlaylist.clear();
      Media.Clear();

      LastSelectedIndices = LvLocalList.SelectedItems.Count > 0
        ? LvLocalList.SelectedIndices.OfType<int>().ToList()
        : Enumerable.Range(0, LvLocalList.Items.Count - 1).ToList();
      var entries = LastSelectedIndices.Select(i => LvLocalList.Items[i])
        .OfType<ListViewItem>().Select(x => x.Tag)
        .OfType<TrackMetadata>().Select(x => x.Path);
      foreach (var path in entries)
      {
        var media = Wmp.newMedia(path);
        Media.Add(media);
        Wmp.currentPlaylist.appendItem(media);
      }
      WmpPlay();
    }

    private void WmpPlay()
    {
      Wmp.controls.play();
      TrackBarTimer.Enabled = true;
      PlayButton.Image = Properties.Resources.Pause;
    }

    private void TrProgress_Scroll(object sender, EventArgs e)
    {
      Wmp.controls.currentPosition = TrProgress.Value;
    }

    private void BtnPrev_Click(object sender, EventArgs e)
    {
      Wmp.controls.previous();
    }

    private void NextButton_Click(object sender, EventArgs e)
    {
      Wmp.controls.next();
    }
  }
}
