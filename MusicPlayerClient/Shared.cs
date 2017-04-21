using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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

    public static bool VerifyPathPort(
      TextBox tbPath, TextBox tbIp, TextBox tbPort,
      out IPEndPoint addr)
    {
      addr = null;
      if (!Directory.Exists(tbPath.Text))
      {
        MessageBox.Show("유효한 MP3 파일 저장 경로가 아닙니다.");
        return false;
      }
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

    public static void BrowseHandler(TextBox tbPath, ListView lvTracks, object sender, EventArgs e)
    {
      var fd = new FolderBrowserDialog();
      fd.RootFolder = Environment.SpecialFolder.MyComputer;

      // TODO: 테스트 코드 제거
      //if (fd.ShowDialog() != DialogResult.OK) return;
      fd.SelectedPath = @"D:\Jeiea\Music\OSTFavorite";

      tbPath.Text = fd.SelectedPath;
      lvTracks.Items.Clear();
      var wmp = new WindowsMediaPlayer();

      var exts = new Regex(@"\.(mp[234a]|m4a|aac|mka|wma|wav|flac)$");
      var audios = Directory.EnumerateFiles(tbPath.Text).Where(x => exts.IsMatch(x));
      foreach (var file in audios)
      {
        var medium = wmp.newMedia(file);
        int.TryParse(medium.getItemInfo("FileSize"), out int size);
        // getItemInfoByAtom은 구현이 안 된듯 하다.
        var meta = new TrackMetadata()
        {
          Title = medium.name,
          Artist = medium.getItemInfo("Artist"),
          Duration = new TimeSpan(Convert.ToInt64(medium.duration * 10000000)),
          FileSize = size,
          Path = file
        };
        lvTracks.Items.Add(new ListViewItem(meta.ListItem) { Tag = meta });
      }
    }
  }
}
