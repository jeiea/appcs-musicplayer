using System;
using System.Windows.Forms;
using WMPLib;

namespace MusicPlayerCommon
{
  public class PlayListViewItemCollection : ListView.ListViewItemCollection
  {
    WindowsMediaPlayer Player;

    public PlayListViewItemCollection(ListView owner, WindowsMediaPlayer player) : base(owner)
    {
      Player = player;
    }

    public override void Clear()
    {
      Player.currentPlaylist.clear();
      base.Clear();
    }

    public override ListViewItem Add(ListViewItem value)
    {
      if (!(value.Tag is TrackMetadata meta))
        throw new ArgumentException("Precondition violation");
      Player.currentPlaylist.appendItem(Player.newMedia(meta.Path));
      value.Tag = Player.currentPlaylist.Item[Player.currentPlaylist.count - 1];
      return base.Add(value);
    }

    public override void Remove(ListViewItem item)
    {
      var media = item.Tag as IWMPMedia;
      if (Player.currentMedia.isIdentical[media])
      {
        MessageBox.Show("현재 재생중인 곡은 삭제할 수 없습니다.");
        return;
      }
      Player.currentPlaylist.removeItem(media);
      base.Remove(item);
    }
  }
}
