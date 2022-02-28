using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Converter;
using DotNetTools.SharpGrabber.Grabbed;

using Microsoft.AspNetCore.Mvc;

using System.Diagnostics;

using VideoLibrary;

namespace Downloader
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient Client = new();
        private static readonly HashSet<string> TempFiles = new HashSet<string>();

        public Form1()
        {
            InitializeComponent();
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            //UseSharpGrabber();
            //UseVideoLibrary();
            UseYoutubeExplode();
        }

        private void UseYoutubeExplode()
        {
            Task.Run(() =>
            {

            });
        }

















        private void UseVideoLibrary()
        {
            Task.Run(() =>
            {
                var youTube = YouTube.Default;
                var video = youTube.GetAllVideos(txtLink.Text);
            });
        }























        private void UseSharpGrabber()
        {
            var grabber = GrabberBuilder.New()
                .UseDefaultServices()
                .AddYouTube()
                .AddVimeo()
                .Build();
            btnDownload.Enabled = false;
            txtLink.Enabled = false;
            Task.Run(async () =>
            {
                try
                {
                    var result = await grabber.GrabAsync(new Uri(txtLink.Text));
                    var info = result.Resource<GrabbedInfo>();
                    var images = result.Resources<GrabbedImage>().ToArray();
                    var mediaFiles = result.Resources<GrabbedMedia>().ToArray();
                    var sortedMediaFiles = mediaFiles.OrderByResolutionDescending().ThenByBitRateDescending().ToArray();
                    var bestVideo = mediaFiles.GetHighestQualityVideo();
                    var bestAudio = mediaFiles.GetHighestQualityAudio();
                    var audioStream = ChooseMonoMedia(result, MediaChannels.Audio);
                    var videoStream = ChooseMonoMedia(result, MediaChannels.Video);
                    var audioPath = await DownloadMedia(audioStream, result);
                    var videoPath = await DownloadMedia(videoStream, result);
                    GenerateOutputFile(audioPath, videoPath, videoStream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).ContinueWith(task =>
            {
                btnDownload.Enabled = true;
                txtLink.Enabled = true;
                prgDownload.Value = 0;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void GenerateOutputFile(string audioPath, string videoPath, GrabbedMedia videoStream)
        {
            FFmpeg.AutoGen.ffmpeg.RootPath = "C:\\Users\\erick\\source\\repos\\Downloader\\Test\\ffmpeg";
            var outputPath = "D:\\Documents\\" + videoStream.Title;
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new Exception("No output path is specified.");
            var merger = new MediaMerger(outputPath);
            merger.AddStreamSource(audioPath, MediaStreamType.Audio);
            merger.AddStreamSource(videoPath, MediaStreamType.Video);
            merger.OutputMimeType = videoStream.Format.Mime;
            merger.OutputShortName = videoStream.Format.Extension;
            merger.Build();
        }

        [RequestSizeLimit(10L * 1024L * 1024L * 1024L)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10L * 1024L * 1024L * 1024L)]
        private async Task<string> DownloadMedia(GrabbedMedia media, IGrabResult grabResult)
        {
            var stopwatch = new Stopwatch();
            var downloadRate = 1048576;
            lblProgress.Invoke(new Action(() => {
                lblProgress.Text = "Esperando...";
                prgDownload.Value = 0;
            }));

            //"http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4" Testing video
            using var response = await Client.GetAsync(media.ResourceUri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var downloadStream = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(downloadStream, null,true,downloadRate, false);

            var path = Path.GetTempFileName();
            TempFiles.Add(path);
            var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, downloadRate, true);

            var read = 0;
            var loopReaded = 0;
            var totalRead = 0L;
            var buffer = new byte[downloadRate];
            CancellationToken token = new();
            token.ThrowIfCancellationRequested();

            lblProgress.Invoke(new Action(() => {
                lblProgress.Text = "Total: " + totalBytes;
                lblSize.Text = (totalBytes / 1000000.0).ToString("0.##") + " MB";
                lblDownloading.Text = media.Channels + " - " + grabResult.Title;
            }));

            do
            {
                stopwatch.Start();
                read = await downloadStream.ReadAsync(buffer, 0, downloadRate);
                var data = new byte[read];
                buffer.ToList().CopyTo(0, data, 0, read);

                await fileStream.WriteAsync(buffer, 0, read);
                totalRead += read;
                loopReaded += read;

                if (stopwatch.ElapsedMilliseconds / 100 > 1)
                {
                    stopwatch.Stop();

                    lblProgress.Invoke(new Action(() =>
                    {
                        var downloadPercentage = totalRead * 1d / (totalBytes * 1d) * 100;
                        lblProgress.Text = "Descargado: " + downloadPercentage.ToString("0.##") + "%";
                        lblDownloaded.Text = (totalRead / 1000000.0).ToString("0.##") + " MB";
                        var speed = ((loopReaded * 1000.0) / stopwatch.ElapsedMilliseconds) / 1000.0;
                        if(speed > 1000)
                        {
                            speed /= 1000;
                            lblSpeed.Text = speed.ToString("0.##") + " MB/sec";
                        }
                        else
                        {
                            lblSpeed.Text = speed.ToString("0.##") + " KB/sec";
                        }

                        prgDownload.Value = Convert.ToInt32(downloadPercentage);
                    }));
                    stopwatch.Reset();
                    loopReaded = 0;
                }
            }
            while (read > 0);
            
            return path;
        }

        private GrabbedMedia ChooseMonoMedia(GrabResult result, MediaChannels channel)
        {
            var resources = result.Resources<GrabbedMedia>()
               .Where(m => m.Channels == channel)
               .ToList();

            if (resources.Count == 0)
                return null;

            return channel == MediaChannels.Audio ? resources.GetHighestQualityAudio() : resources.GetHighestQualityVideo();
        }
    }
}