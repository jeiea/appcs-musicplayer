using MusicPlayerCommon;
using System;
using System.Collections.Generic;
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
  public partial class ClientForm : Form
  {
    Task Worker;
    CancellationTokenSource WorkerAbort;
    MyBufferBlock<object> WriteQueues = new MyBufferBlock<object>();
    WindowsMediaPlayer Wmp = new WindowsMediaPlayer();
    List<IWMPMedia> Media = new List<IWMPMedia>();
    List<int> LastSelectedIndices = new List<int>();
    System.Windows.Forms.Timer TrackBarTimer =
      new System.Windows.Forms.Timer() { Interval = 1000 };
    TransparentLabel LbProgress;
    PlayListViewItemCollection Playlist;
    MyDispatcher Dispatcher;
    string ServerMessage;

    public ClientForm()
    {
      Dispatcher = new MyDispatcher();
      AppDomain.CurrentDomain.UnhandledException += CommonBehavior.OnUnhandledException;

      InitializeComponent();
      BtnBrowse.Click += (s, e) => RefreshLocalTracks();
      TbIp.Text = CommonBehavior.GetLocalIPAddress().ToString();
      Playlist = new PlayListViewItemCollection(LvLocalList, Wmp);

      // Progress label initialization
      LbProgress = new TransparentLabel()
      {
        Anchor = AnchorStyles.None,
        ForeColor = Color.Black,
        Parent = PbDownload,
        Location = new Point(-110, 4),
        Size = new Size(600, 15),
        Visible = false,
        TextAlign = ContentAlignment.MiddleCenter
      };

      Wmp.currentPlaylist = Wmp.newPlaylist("no title", "");
      Wmp.CurrentItemChange += OnWmpMediaChange;
      Wmp.PlayStateChange += OnWmpPlayStateChange;
      Wmp.MediaError += OnWmpMediaError;
      TrackBarTimer.Tick += TrackBarUpdateTick;
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
      if (Wmp.playState == WMPPlayState.wmppsPaused ||
          Wmp.playState == WMPPlayState.wmppsStopped)
      {
        TrackBarTimer.Enabled = false;
        PlayButton.Image = Properties.Resources.Play;
        TbPath.ReadOnly = false;
        TbPath.BackColor = Color.LightGreen;
      }
      else if (Wmp.playState == WMPPlayState.wmppsPlaying)
      {
        TrackBarTimer.Enabled = true;
        PlayButton.Image = Properties.Resources.Pause;
        TbPath.ReadOnly = true;
        TbPath.BackColor = DefaultBackColor;
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
      Dispatcher.Post(() => { LbProgress.Text = $"{ServerMessage}: {ratio}%"; });
    }

    private async Task ProcessServerMessage(NetworkStream stream)
    {
      switch (await stream.ReadObjAsync(ReportProgress).ConfigureAwait(false))
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
          Dispatcher.Post(LbProgress.Hide);
          break;
        case Announcement msg:
          ServerMessage = msg.Message;
          ReportProgress(0);
          Dispatcher.Post(LbProgress.Show);
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

    private void BtnPrev_Click(object sender, EventArgs e)
    {
      if (Wmp.currentMedia.isIdentical[Wmp.currentPlaylist.Item[0]])
      {
        MessageBox.Show("재생목록 처음 곡입니다.");
        return;
      }
      Wmp.controls.previous();
      TrackBarUpdateTick(null, null);
    }

    private void NextButton_Click(object sender, EventArgs e)
    {
      if (Wmp.currentMedia.isIdentical[Wmp.currentPlaylist.Item[Wmp.currentPlaylist.count - 1]])
      {
        MessageBox.Show("재생목록 마지막 곡입니다.");
        return;
      }
      Wmp.controls.next();
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
  }

  // http://stackoverflow.com/a/608256
  public class TransparentLabel : Label
  {
    public TransparentLabel()
    {
      SetStyle(ControlStyles.Opaque, true);
      SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
    }
    protected override CreateParams CreateParams
    {
      get
      {
        CreateParams parms = base.CreateParams;
        parms.ExStyle |= 0x20;  // Turn on WS_EX_TRANSPARENT
        return parms;
      }
    }
  }
}
