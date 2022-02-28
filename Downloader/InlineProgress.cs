using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YoutubeExplode.Videos.Streams;

namespace Downloader
{
    public class InlineProgress : IProgress<double>, IDisposable
    {
        private ProgressBar prgDownload;
        private Label lblProgress;
        private Label lblDownloaded;
        private FileSize size;

        public InlineProgress(FileSize size, ProgressBar prgDownload, Label lblProgress, Label lblDownloaded)
        {
            this.prgDownload = prgDownload;
            this.lblProgress = lblProgress;
            this.lblDownloaded = lblDownloaded;
            this.size = size;
        }

        public void Dispose()
        {
            prgDownload.Invoke(() => {
                prgDownload.Value = 0;
            });
        }

        public void Report(double value)
        {
            var percentage = value * 100;

            prgDownload.Invoke(() =>
            {
                lblProgress.Text = "Descargado: " + percentage.ToString("0.##") + "%";
                lblDownloaded.Text = (size.MegaBytes * value).ToString("0.##") + " MB";
                prgDownload.Value = Convert.ToInt32(percentage);
            });
        }
    }
}
