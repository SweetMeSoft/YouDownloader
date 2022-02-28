using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YoutubeExplode.Videos.Streams;

namespace Downloader
{
    public class InlineProgress : IProgress<double>, IDisposable
    {
        private readonly ProgressBar prgDownload;
        private readonly Label lblProgress;
        private readonly Label lblDownloaded;
        private readonly Label lblSpeed;
        private readonly FileSize size;
        private readonly Stopwatch stopwatch = new();
        private double lastReaded = 0;

        public InlineProgress(FileSize size, ProgressBar prgDownload, Label lblProgress, Label lblDownloaded, Label lblSpeed)
        {
            this.prgDownload = prgDownload;
            this.lblProgress = lblProgress;
            this.lblDownloaded = lblDownloaded;
            this.lblSpeed = lblSpeed;
            this.size = size;
            stopwatch.Start();
        }

        public void Dispose()
        {
            prgDownload.Invoke(() => {
                prgDownload.Value = 0;
            });
        }

        public void Report(double value)
        {
            if (stopwatch.ElapsedMilliseconds > 200)
            {
                var percentage = value * 100;

                prgDownload.Invoke(() =>
                {
                    lblProgress.Text = "Descargado: " + percentage.ToString("0.##") + "%";
                    lblDownloaded.Text = (size.MegaBytes * value).ToString("0.##") + " MB";
                    prgDownload.Value = Convert.ToInt32(percentage);

                    var readed = (size.Bytes * value) - lastReaded;
                    var speed = ((readed * 1000.0) / stopwatch.ElapsedMilliseconds) / 1000.0;
                    if (speed > 1000)
                    {
                        speed /= 1000;
                        lblSpeed.Text = speed.ToString("0.##") + " MB/sec";
                    }
                    else
                    {
                        lblSpeed.Text = speed.ToString("0.##") + " KB/sec";
                    }
                });
                stopwatch.Restart();
                lastReaded = size.Bytes * value;
            }
        }
    }
}
