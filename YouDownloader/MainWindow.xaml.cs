using DotNetTools.SharpGrabber.Converter;

using Syroot.Windows.IO;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using YouDownloader.Models;

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
        public ObservableCollection<VideoInfo> videoList = new();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            itcVideos.ItemsSource = videoList;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var link = txtLink.Text;
            Video videoInfo = null;
            var youtube = new YoutubeClient();
            IStreamInfo videoStream = null;
            IStreamInfo audioStream = null;
            Task.Run(async () =>
            {
                videoInfo = await youtube.Videos.GetAsync(link);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoInfo.Id);
                videoStream = streamManifest.GetVideoStreams().GetWithHighestVideoQuality();
                audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            }).ContinueWith(task =>
            {
                if (videoInfo != null)
                {
                    videoList.Add(new VideoInfo()
                    {
                        Title = videoInfo.Title,
                        Description = videoInfo.Description,
                        Size = GetSize(audioStream, videoStream),
                        UrlThumbnail = videoInfo.Thumbnails[0].Url.Substring(0, videoInfo.Thumbnails[0].Url.IndexOf("?")),
                        Video = videoInfo
                    });
                }
                //itcVideos.ItemsSource = videoList;
                txtLink.Text = "";
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private string GetSize(IStreamInfo audioStream, IStreamInfo videoStream)
        {
            if (videoStream.Size.MegaBytes + audioStream.Size.MegaBytes > 1000)
            {
                return (audioStream.Size.GigaBytes + videoStream.Size.GigaBytes).ToString("0.##") + " GB";
            }

            return (audioStream.Size.MegaBytes + videoStream.Size.MegaBytes).ToString("0.##") + " MB";
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            var audioPath = "";
            var videoPath = "";
            btnDownload.IsEnabled = false;
            Task.Run(async () =>
            {
                for (var i = 0; i < videoList.Count; i++)
                {
                    try
                    {
                        var youtube = new YoutubeClient();
                        var videoInfo = await youtube.Videos.GetAsync(videoList[i].Video.Id);
                        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoInfo.Id);
                        var videoStream = streamManifest.GetVideoStreams().GetWithHighestVideoQuality();
                        var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                        audioPath = await Download(youtube, videoInfo, audioStream);
                        videoPath = await Download(youtube, videoInfo, videoStream);
                        GenerateOutputFile(videoInfo, audioPath, videoPath, videoStream);
                        itcVideos.Dispatcher.Invoke(() =>
                        {
                            videoList.Remove(videoList[i]);
                            i--;
                        });
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
            var path = Path.GetTempFileName();
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

            var filePath = GetFilePath(outputPath, videoInfo, videoStream, 0);
            var merger = new MediaMerger(filePath);
            merger.AddStreamSource(audioPath, MediaStreamType.Audio);
            merger.AddStreamSource(videoPath, MediaStreamType.Video);
            merger.OutputMimeType = "video/" + videoStream.Container.Name;
            merger.OutputShortName = videoStream.Container.Name;
            merger.Build();
        }

        private string GetFilePath(string outputPath, Video videoInfo, IStreamInfo videoStream, int position)
        {
            string? path;
            if (position > 0)
            {
                path = outputPath + "/" + videoInfo.Title + " (" + position + ")." + videoStream.Container.Name;
            }
            else {
                path = outputPath + "/" + videoInfo.Title + "." + videoStream.Container.Name;
            }

            if (File.Exists(path))
            {
                return GetFilePath(outputPath, videoInfo, videoStream, ++position);
            }

            return path;
        }
    }
}
