using MusicPlayerCommon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;

namespace MusicPlayerClient
{
  public enum RepeatType { Sequantial, Random, OnlyRepeat, WholeWrap }

  public partial class ClientForm : Form
  {
    Task Worker;
    CancellationTokenSource WorkerAbort;
    MyBufferBlock<object> WriteQueues = new MyBufferBlock<object>();
    WindowsMediaPlayer Wmp = new WindowsMediaPlayer();
    System.Windows.Forms.Timer TrackBarTimer =
      new System.Windows.Forms.Timer() { Interval = 1000 };
    PlayListViewItemCollection Playlist;
    MyDispatcher Dispatcher;
    string ServerMessage;
    Random Rand = new Random();
    WMPPlayState PlayState = WMPPlayState.wmppsReady;

    public ClientForm()
    {
      Dispatcher = new MyDispatcher();
      AppDomain.CurrentDomain.UnhandledException += CommonBehavior.OnUnhandledException;

      InitializeComponent();
      BtnBrowse.Click += (s, e) => RefreshLocalTracks();
      TbIp.Text = CommonBehavior.GetLocalIPAddress().ToString();
      Playlist = new PlayListViewItemCollection(LvLocalList, Wmp);
      CbRepeat.SelectedIndex = 0;

      InitializeRepeatTypeCombobox();

      Wmp.currentPlaylist = Wmp.newPlaylist("no title", "");
      Wmp.CurrentItemChange += OnWmpMediaChange;
      Wmp.PlayStateChange += OnWmpPlayStateChange;
      Wmp.MediaError += OnWmpMediaError;
      TrackBarTimer.Tick += TrackBarUpdateTick;
    }

    private void InitializeRepeatTypeCombobox()
    {
      var comboItems = new BindingList<Tuple<string, RepeatType>>();
      comboItems.Add(new Tuple<string, RepeatType>("순차재생", RepeatType.Sequantial));
      comboItems.Add(new Tuple<string, RepeatType>("랜덤재생", RepeatType.Random));
      comboItems.Add(new Tuple<string, RepeatType>("한곡반복", RepeatType.OnlyRepeat));
      comboItems.Add(new Tuple<string, RepeatType>("전체반복", RepeatType.WholeWrap));

      CbRepeat.DataSource = comboItems;
      CbRepeat.DisplayMember = "Item1";
      CbRepeat.ValueMember = "Item2";
    }

    private void OnWmpMediaError(object pMediaObject)
    {
      string name = Wmp.currentMedia.name;
      if (string.IsNullOrEmpty(name))
        name = Wmp.currentMedia.sourceURL;
      MessageBox.Show("음악 파일이 이상합니다. 재생목록에서 제거합니다.\r\n" + name);
      for (int i = Wmp.currentPlaylist.count - 1; i >= 0; i--)
      {
        if (Wmp.currentMedia.isIdentical[Wmp.currentPlaylist.Item[i]])
        {
          Wmp.currentPlaylist.removeItem(Wmp.currentMedia);
          Playlist.RemoveAt(i);
          Wmp.controls.play();
        }
      }
    }

    private void OnWmpPlayStateChange(int NewState)
    {
      var media = Wmp.currentMedia;
      switch (Wmp.playState)
      {
        case WMPPlayState.wmppsPaused:
        case WMPPlayState.wmppsStopped:
        case WMPPlayState.wmppsReady:
          TrackBarTimer.Enabled = false;
          PlayButton.Image = Properties.Resources.Play;
          TbPath.ReadOnly = false;
          TbPath.BackColor = Color.LightGreen;
          PlayState = WMPPlayState.wmppsPaused;
          break;
        case WMPPlayState.wmppsPlaying:
          TrackBarTimer.Enabled = true;
          PlayButton.Image = Properties.Resources.Pause;
          TbPath.ReadOnly = true;
          TbPath.BackColor = DefaultBackColor;
          PlayState = WMPPlayState.wmppsPlaying;
          break;
        case WMPPlayState.wmppsMediaEnded:
          DetermineRepeat();
          break;
      }
    }

    private void DetermineRepeat()
    {
      var count = Wmp.currentPlaylist.count;
      switch (CbRepeat.SelectedValue as RepeatType? ?? RepeatType.Sequantial)
      {
        case RepeatType.Sequantial:
          break;
        case RepeatType.Random:
          Wmp.controls.playItem(Wmp.currentPlaylist.Item[Rand.Next(count)]);
          break;
        case RepeatType.OnlyRepeat:
          Wmp.controls.previous();
          break;
        case RepeatType.WholeWrap:
          var lastItem = Wmp.currentPlaylist.Item[count - 1];
          if (Wmp.currentMedia.isIdentical[lastItem])
          {
            Wmp.controls.play();
          }
          break;
      }
    }

    private void RefreshLocalTracks()
    {
      CommonBehavior.BrowseHandler(TbPath, Playlist);
    }

    private void TrackBarUpdateTick(object sender, EventArgs e)
    {
      double cur = Convert.ToDouble(Wmp.controls.currentPosition);
      TrProgress.Value = (int)(cur * 1000 / TrackBarTimer.Interval);
    }

    private void OnWmpMediaChange(object obj)
    {
      var media = Wmp.currentMedia;
      LbPlayerStatus.Text = media.name;
      TrProgress.Maximum = (int)(Convert.ToDouble(media.duration) * 1000 / TrackBarTimer.Interval);
      TrackBarUpdateTick(null, null);
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
      if (!CommonBehavior.VerifyEndpoint(TbIp, TbPort, out IPEndPoint addr)) return;

      WorkerAbort = new CancellationTokenSource();
      Worker = new Task(new Action(() => TcpWorker(addr)), WorkerAbort.Token);
      Worker.Start();
      BtnToggle.Text = "Connecting...";
    }

    void ReflectConnection()
    {
      BtnToggle.Text = "Disconnect";
      BtnToggle.ForeColor = Color.Red;
      TbIp.ReadOnly = TbPort.ReadOnly = true;
    }

    void ReflectDisconnection()
    {
      Worker = null;
      BtnToggle.Text = "Connect";
      BtnToggle.ForeColor = Color.Black;
      TbIp.ReadOnly = TbPort.ReadOnly = false;
      lvRemoteList.Items.Clear();
    }

    async void TcpWorker(IPEndPoint addr)
    {
      var client = new TcpClient();
      try
      {
        var token = WorkerAbort.Token;
        token.ThrowIfCancellationRequested();
        await client.ConnectAsync(addr.Address, addr.Port).ConfigureAwait(false);

        Dispatcher.Post(ReflectConnection);
        var stream = client.GetStream();
        WorkerAbort.Token.Register(() => { stream.Close(); });

        ProcessWriteQueue(stream).IgnoreExceptions();
        while (!token.IsCancellationRequested)
          await ProcessServerMessage(stream);
      }
      catch (ObjectDisposedException) { }
      catch (Exception e)
      {
        MessageBox.Show(e.Message);
      }
      finally
      {
        client.Close();
        Dispatcher.Post(ReflectDisconnection);
      }
    }

    void ReportProgress(double ratio)
    {
      if (ServerMessage == null) return;
      Dispatcher.Post(() =>
      {
        PbDownload.Value = (int)(ratio * 1000000);
        Text = string.Format("Music Player Client - {0}: {1:0.0%}", ServerMessage, ratio);
      });
    }

    private async Task ProcessServerMessage(NetworkStream stream)
    {
      switch (await stream.ReadObjAsync(ReportProgress))
      {
        case TrackMetadata[] metas:
          var items = metas.Select(x => new ListViewItem(x.ListItem)).ToArray();
          Dispatcher.Post(() =>
          {
            lvRemoteList.Items.Clear();
            lvRemoteList.Items.AddRange(items);
          });
          break;
        case FileBlob file:
          var repo = await Dispatcher.Send(() => TbPath.Text);
          var path = Path.Combine(repo, file.Name);
          var preExists = File.Exists(path);
          try
          {
            using (FileStream fs = File.Create(path))
            {
              await fs.WriteAsync(file.Body, 0, file.Body.Length).ContinueWith(async t =>
              {
                await t;
                fs.Close();
                if (!preExists) Dispatcher.Post(() =>
                {
                  var meta = CommonBehavior.GetMetadata(Wmp, path);
                  Playlist.Add(new ListViewItem(meta.ListItem) { Tag = meta });
                });
              }).ConfigureAwait(false);
            }
          }
          catch (Exception e)
          {
            MessageBox.Show(e.Message);
          }
          ServerMessage = null;
          Dispatcher.Post(() => { Text = "Music Player Client"; });
          break;
        case Announcement msg:
          ServerMessage = msg.Message;
          ReportProgress(0);
          break;
      }
    }

    private async Task ProcessWriteQueue(NetworkStream stream)
    {
      var token = WorkerAbort.Token;
      while (!token.IsCancellationRequested)
      {
        var obj = await WriteQueues.ReceiveAsync(token);
        await stream.WriteObjAsync(obj);
      }
    }

    private void PlayButton_Click(object sender, EventArgs e)
    {
      if (Playlist.Count <= 0) return;
      if (Wmp.playState == WMPPlayState.wmppsPlaying) Wmp.controls.pause();
      else Wmp.controls.play();
    }

    private void TrProgress_Scroll(object sender, EventArgs e)
    {
      Wmp.controls.currentPosition = (double)TrProgress.Value / 1000 * TrackBarTimer.Interval;
    }

    private void OnNavigation(object sender, EventArgs e)
    {
      var isNext = sender == BtnNext;
      switch (CbRepeat.SelectedValue as RepeatType?)
      {
        case RepeatType.Sequantial:
          int idx = isNext ? Wmp.currentPlaylist.count - 1 : 0;
          string reason = isNext ? "마지막" : "처음";
          if (Wmp.currentMedia.isIdentical[Wmp.currentPlaylist.Item[idx]]) {
            MessageBox.Show($"재생목록 {reason} 곡입니다.");
            return;
          }
          if (isNext) Wmp.controls.next();
          else Wmp.controls.previous();
          break;
        case RepeatType.Random:
          for (int i = Rand.Next(1, Wmp.currentPlaylist.count); i > 0; i--)
            Wmp.controls.next();
          break;
        case RepeatType.OnlyRepeat:
          Wmp.controls.currentPosition = 0;
          break;
        case RepeatType.WholeWrap:
          if (isNext) Wmp.controls.next();
          else Wmp.controls.previous();
          break;
      }
      TrackBarUpdateTick(null, null);
    }

    private void BtnDownload_Click(object sender, EventArgs e)
    {
      if (!Directory.Exists(TbPath.Text))
      {
        MessageBox.Show("파일 다운로드 위치가 정확하지 않습니다.",
          "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }
      foreach (var idx in lvRemoteList.SelectedIndices.OfType<int>())
      {
        WriteQueues.Enqueue(new DownloadRequest(idx));
      }
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
      foreach (ListViewItem item in LvLocalList.SelectedItems)
      {
        Playlist.Remove(item);
      }
    }

    private void TbPath_TextChanged(object sender, EventArgs e)
    {
      if (Directory.Exists(TbPath.Text))
      {
        RefreshLocalTracks();
        TbPath.BackColor = Color.LightGreen;
      }
      else
      {
        Playlist.Clear();
        TbPath.BackColor = Color.Pink;
      }
    }

    private void BtnUpload_Click(object sender, EventArgs e)
    {
      var uploads = LvLocalList.SelectedItems
        .OfType<ListViewItem>().Select(x => x.Tag)
        .OfType<IWMPMedia>().Select(x => x.sourceURL);
      foreach (var path in uploads)
      {
        var file = new FileBlob()
        {
          Name = Path.GetFileName(path),
          Body = File.ReadAllBytes(path)
        };
        WriteQueues.Enqueue(file);
      }
    }
  }
}