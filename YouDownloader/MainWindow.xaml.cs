using DotNetTools.SharpGrabber.Converter;

using Syroot.Windows.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YouDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            var link = txtLink.Text;
            var audioPath = "";
            var videoPath = "";
            btnDownload.IsEnabled = false;
            txtLink.IsEnabled = false;
            Task.Run(async () =>
            {
                try
                {
                    var youtube = new YoutubeClient();

                    var videoInfo = await youtube.Videos.GetAsync(link);
                    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoInfo.Id);
                    var videoStream = streamManifest.GetVideoStreams().GetWithHighestVideoQuality();
                    var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                    audioPath = await Download(youtube, videoInfo, audioStream);
                    videoPath = await Download(youtube, videoInfo, videoStream);
                    GenerateOutputFile(videoInfo, audioPath, videoPath, videoStream);
                }
                catch (Exception ex)
                {
                    var a = ex;
                }
                finally
                {
                    File.Delete(audioPath);
                    File.Delete(videoPath);
                }
            }).ContinueWith(task =>
            {
                btnDownload.IsEnabled = true;
                txtLink.IsEnabled = true;
                prgDownload.Value = 0;
                lblProgress.Content = "Finished...  ";
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task<string> Download(YoutubeClient youtube, Video videoInfo, IStreamInfo stream)
        {
            lblDownloading.Dispatcher.Invoke(() =>
            {
                lblDownloading.Content = stream.Container.Name + " - " + videoInfo.Title;
                lblSize.Content = stream.Size.ToString();
            });
            var path = System.IO.Path.GetTempFileName();
            using var progress = new InlineProgress(stream.Size, prgDownload, lblProgress, lblDownloaded, lblSpeed);
            await youtube.Videos.Streams.DownloadAsync(stream, path, progress);
            return path;
        }

        private void GenerateOutputFile(Video videoInfo, string audioPath, string videoPath, IStreamInfo videoStream)
        {
            MediaLibrary.Load(@"C:\Users\erick\source\repos\Downloader\YouDownloader\ffmpeg");
            var outputPath = new KnownFolder(KnownFolderType.Downloads).Path + "/YouDownloader";
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var filePath = outputPath + "/" + videoInfo.Title + "." + videoStream.Container.Name;
            var merger = new MediaMerger(filePath);
            merger.AddStreamSource(audioPath, MediaStreamType.Audio);
            merger.AddStreamSource(videoPath, MediaStreamType.Video);
            merger.OutputMimeType = "video/" + videoStream.Container.Name;
            merger.OutputShortName = videoStream.Container.Name;
            merger.Build();
        }
    }
}
