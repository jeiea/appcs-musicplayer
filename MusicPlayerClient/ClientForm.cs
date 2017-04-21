using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MusicPlayerCommon;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MusicPlayerClient
{
  public partial class ClientForm : Form
  {
    CancellationTokenSource WorkerAbort;
    Task Worker;

    public ClientForm()
    {
      InitializeComponent();
      AppDomain.CurrentDomain.UnhandledException += CommonBehavior.OnUnhandledException;
      BtnBrowse.Click += (s, e) =>
      CommonBehavior.BrowseHandler(TbPath, lvLocalList, s, e);
      TbIp.Text = CommonBehavior.GetLocalIPAddress().ToString();
    }

    private void BtnConnect_Click(object sender, EventArgs e)
    {
      if (!CommonBehavior.VerifyPathPort(TbPath, TbIp, TbPort, out IPEndPoint addr))
        return;

      TbIp.ReadOnly = TbPort.ReadOnly = TbPath.ReadOnly = true;
      WorkerAbort = new CancellationTokenSource();
      Worker = new Task(new Action(() => TcpWorker(addr)), WorkerAbort.Token);
      Worker.Start();
      BtnToggle.Text = "Stop";
    }

    async void TcpWorker(IPEndPoint addr)
    {
      var client = new TcpClient();
      await client.ConnectAsync(addr.Address, addr.Port);

      var stream = client.GetStream();
      var formatter = new BinaryFormatter();
      while (!WorkerAbort.IsCancellationRequested)
      {
        var metas = formatter.Deserialize(stream) as TrackMetadata[];
        var items = metas.Select(x => new ListViewItem(x.ListItem)).ToArray();
        Invoke(new Action(() => {
          lvRemoteList.Clear();
          lvRemoteList.Items.AddRange(items);
        }));
      }
    }
  }
}
