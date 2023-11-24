using DotNetTools.SharpGrabber.Converter;

using Syroot.Windows.IO;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using YouDownloader.Models;

using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
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
        private readonly YoutubeClient youtube = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            itcVideos.ItemsSource = videoList;
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            btnAdd.IsEnabled = false;
            txtLink.IsEnabled = false;
            await Add();
            txtLink.Text = "";
            txtLink.IsEnabled = true;
            btnAdd.IsEnabled = true;
        }

        private async Task Add()
        {
            if (txtLink.Text.ToLower().Contains("channel"))
            {
                var channel = await youtube.Channels.GetAsync(txtLink.Text);
                await AddChannelToList(channel);
            }
            else
            {
                if (txtLink.Text.ToLower().Contains("/c/"))
                {
                    var channel = await youtube.Channels.GetBySlugAsync(txtLink.Text);
                    await AddChannelToList(channel);
                }
                else
                {
                    if (txtLink.Text.ToLower().Contains("playlist"))
                    {
                        await foreach (var video in youtube.Playlists.GetVideosAsync(txtLink.Text))
                        {
                            await AddItemToList(video.Url);
                        }
                    }
                    else
                    {
                        await AddItemToList(txtLink.Text);
                    }
                }
            }
        }

        private async Task AddChannelToList(Channel channel)
        {
            var uploads = youtube.Channels.GetUploadsAsync(channel.Id);
            var videos = await uploads.ToListAsync();
            foreach (var video in videos)
            {
                await AddItemToList(video.Url);
            }
        }

        private async Task AddItemToList(string link)
        {
            try
            {
                var videoInfo = await youtube.Videos.GetAsync(link);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoInfo.Id);
                var videoStream = streamManifest.GetVideoStreams().GetWithHighestVideoQuality();
                var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (videoInfo != null && audioStream != null && videoStream != null)
                {
                    videoList.Add(new VideoInfo()
                    {
                        Title = videoInfo.Title,
                        Description = videoInfo.Description,
                        Size = GetSize(audioStream, videoStream),
                        Date = videoInfo.UploadDate.ToString("yyyy-MM-dd"),
                        UrlThumbnail = videoInfo.Thumbnails[0].Url.Substring(0, videoInfo.Thumbnails[0].Url.IndexOf("?")),
                        Video = videoInfo
                    });
                }
                else
                {
                    Console.WriteLine("Ups");
                }
            }catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            IStreamInfo audioStream;
            IStreamInfo videoStream = null;
            var audioPath = "";
            var videoPath = "";
            var downloadAudio = chkDownloadAudio.IsChecked ?? false;
            var downloadVideo = chkDownloadVideo.IsChecked ?? false;
            var mergeAudioVideo = chkMergeAudioVideo.IsChecked ?? false;
            var downloadSubtitles = chkDownloadSubtitles.IsChecked ?? false;
            var downloadSummary = chkDownloadSummary.IsChecked ?? false;

            btnDownload.IsEnabled = false;
            chkDownloadAudio.IsEnabled = false;
            chkDownloadVideo.IsEnabled = false;
            chkMergeAudioVideo.IsEnabled = false;
            chkDownloadSubtitles.IsEnabled = false;
            chkDownloadSummary.IsEnabled = false;

            for (var i = 0; i < videoList.Count; i++)
            {
                try
                {
                    var youtube = new YoutubeClient();
                    var videoInfo = await youtube.Videos.GetAsync(videoList[i].Video.Id);
                    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoInfo.Id);
                    audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                    videoStream = streamManifest.GetVideoStreams().GetWithHighestVideoQuality();

                    if (downloadAudio)
                    {
                        audioPath = await DownloadStream(youtube, videoInfo, audioStream);
                    }

                    if (downloadVideo)
                    {
                        videoPath = await DownloadStream(youtube, videoInfo, videoStream);
                    }

                    if (mergeAudioVideo)
                    {
                        GenerateOutputFile(videoInfo, audioPath, videoPath, videoStream);
                    }
                    else
                    {
                        if (downloadAudio)
                        {
                            CopyFile(videoInfo, audioStream, audioPath);
                        }

                        if (downloadVideo)
                        {
                            CopyFile(videoInfo, videoStream, videoPath);
                        }
                    }

                    if (downloadSubtitles)
                    {
                        DownloadSubtitles(videoInfo);
                    }

                    if (downloadSummary)
                    {
                        DownloadSummaryText(videoInfo, videoStream);
                    }

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
                    if (downloadAudio)
                    {
                        File.Delete(audioPath);
                    }

                    if (downloadVideo)
                    {
                        File.Delete(videoPath);
                    }
                }
            }

            btnDownload.IsEnabled = true;
            txtLink.IsEnabled = true;
            chkDownloadAudio.IsEnabled = true;
            chkDownloadVideo.IsEnabled = true;
            chkMergeAudioVideo.IsEnabled = true;
            chkDownloadSubtitles.IsEnabled = true;
            chkDownloadSummary.IsEnabled = true;
            prgDownload.Value = 0;
            lblProgress.Content = "Finished...  ";
            lblDownloading.Content = "";
            lblSize.Content = "";
            lblDownloaded.Content = "";
            lblSpeed.Content = "";
        }

        private async Task<string> DownloadStream(YoutubeClient youtube, Video videoInfo, IStreamInfo stream)
        {
            lblDownloading.Content = stream.Container.Name + " - " + videoInfo.Title;
            lblSize.Content = stream.Size.ToString();

            var path = Path.GetTempFileName();
            using var progress = new InlineProgress(stream.Size, prgDownload, lblProgress, lblDownloaded, lblSpeed);
            await youtube.Videos.Streams.DownloadAsync(stream, path, progress);
            return path;
        }

        private async void DownloadSubtitles(Video videoInfo)
        {
            var outputPath = new KnownFolder(KnownFolderType.Downloads).Path + "/YouDownloader";
            var filePath = GetFilePath(outputPath, videoInfo, 0);
            try
            {
                var trackManifest = await youtube.Videos.ClosedCaptions.GetManifestAsync(videoInfo.Id);
                var trackInfo = trackManifest.GetByLanguage("es");
                await youtube.Videos.ClosedCaptions.DownloadAsync(trackInfo, filePath + ".srt");
            }
            catch (Exception e)
            {
                await File.WriteAllTextAsync(filePath + ".txt", e.Message);
            }
        }

        private async void DownloadSummaryText(Video videoInfo, IStreamInfo videoStream)
        {
            var outputPath = new KnownFolder(KnownFolderType.Downloads).Path + "/YouDownloader";
            var filePath = GetFilePath(outputPath, videoInfo, 0);
            try
            {
                var trackManifest = await youtube.Videos.ClosedCaptions.GetManifestAsync(videoInfo.Id);
                var trackInfo = trackManifest.GetByLanguage("es");
                var track = await youtube.Videos.ClosedCaptions.GetAsync(trackInfo);
                var subtitles = "";
                foreach (var caption in track.Captions)
                {
                    if (!caption.Text.Contains('['))
                    {
                        subtitles += caption.Text + " ";
                    }
                }

                await File.WriteAllTextAsync(filePath + ".txt", subtitles.Replace("\n", " "));
            }
            catch (Exception e)
            {
                await File.WriteAllTextAsync(filePath + ".txt", e.Message);
            }
        }

        private void GenerateOutputFile(Video videoInfo, string audioPath, string videoPath, IStreamInfo videoStream)
        {
            prgDownload.IsIndeterminate = true;
            lblProgress.Content = "Generando archivo final...";
            MediaLibrary.Load("ffmpeg/");
            var outputPath = new KnownFolder(KnownFolderType.Downloads).Path + "/YouDownloader";
            var filePath = GetFilePath(outputPath, videoInfo, 0) + "." + videoStream.Container.Name;
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var merger = new MediaMerger(filePath);
            merger.AddStreamSource(audioPath, MediaStreamType.Audio);
            merger.AddStreamSource(videoPath, MediaStreamType.Video);
            merger.OutputMimeType = "video/" + videoStream.Container.Name;
            merger.OutputShortName = videoStream.Container.Name;
            merger.Build();
            prgDownload.IsIndeterminate = false;
            prgDownload.Value = 0;
        }

        private string GetFilePath(string outputPath, Video videoInfo, int position)
        {
            string? path;
            //var title = Regex.Replace(videoInfo.Title, @"[^0-9a-zA-Z\._\- ]", "");
            var title = videoInfo.Title
                .Replace(":", "")
                .Replace("?", "")
                .Replace("/", "")
                .Replace("\"", "")
                .Replace("*", "")
                .Replace("\\", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("|", "");
            if (position > 0)
            {
                path = outputPath + "/[" + videoInfo.UploadDate.ToString("yyyy-MM-dd") + "] " + title + " (" + position + ")";
            }
            else
            {
                path = outputPath + "/[" + videoInfo.UploadDate.ToString("yyyy-MM-dd") + "] " + title;
            }

            if (File.Exists(path))
            {
                return GetFilePath(outputPath, videoInfo, ++position);
            }

            return path;
        }

        private string GetSize(IStreamInfo audioStream, IStreamInfo videoStream)
        {
            if (videoStream.Size.MegaBytes + audioStream.Size.MegaBytes > 1000)
            {
                return (audioStream.Size.GigaBytes + videoStream.Size.GigaBytes).ToString("0.##") + " GB";
            }

            return (audioStream.Size.MegaBytes + videoStream.Size.MegaBytes).ToString("0.##") + " MB";
        }

        private void chkDownloadAudio_Unchecked(object sender, RoutedEventArgs e)
        {
            chkMergeAudioVideo.IsChecked = false;
            chkMergeAudioVideo.IsEnabled = false;
        }

        private void chkDownloadVideo_Checked(object sender, RoutedEventArgs e)
        {
            if (chkDownloadAudio != null && chkDownloadVideo != null && chkMergeAudioVideo != null)
            {
                if (chkDownloadAudio.IsChecked == true && chkDownloadVideo.IsChecked == true)
                {
                    chkMergeAudioVideo.IsEnabled = true;
                }
            }
        }

        private void CopyFile(Video videoInfo, IStreamInfo stream, string sourcePath)
        {
            var outputPath = new KnownFolder(KnownFolderType.Downloads).Path + "/YouDownloader";
            var filePath = GetFilePath(outputPath, videoInfo, 0) + "." + stream.Container.Name;
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            File.Copy(sourcePath, filePath);
        }
    }
}
