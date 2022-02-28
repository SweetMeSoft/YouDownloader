using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YoutubeExplode.Videos;

namespace YouDownloader.Models
{
    public class VideoInfo
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string Size { get; set; }

        public string UrlThumbnail { get; set; }

        public Video Video { get; set; }
    }
}
