using System;
using System.Collections.Generic;
using System.Text;

namespace MusicPlayerCommon
{
  [Serializable]
  public class TrackMetadata
  {
    public string Title;
    public string Artist;
    public TimeSpan Duration;
    public int FileSize;

    [NonSerialized]
    public string Path;

    public string Bitrate
    {
      get { return $"{(FileSize / Convert.ToInt32(Duration.TotalSeconds) * 8 / 1024).ToString()}kbps"; }
    }

    public string[] ListItem
    {
      get { return new string[] { "", Title, Artist, Duration.ToString(), Bitrate }; }
    }
  }
}
