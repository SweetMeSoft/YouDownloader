using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Converter;
using DotNetTools.SharpGrabber.Grabbed;

using System;

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
            var grabber = GrabberBuilder.New()
                .UseDefaultServices()
                .AddYouTube()
                .Build();
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
                }catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        private void GenerateOutputFile(string audioPath, string videoPath, GrabbedMedia videoStream)
        {
            FFmpeg.AutoGen.ffmpeg.RootPath = "C:\\Users\\erick\\source\\repos\\Downloader\\Test\\ffmpeg";
            var outputPath = "D:\\Documents\\video";
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new Exception("No output path is specified.");
            var merger = new MediaMerger(outputPath);
            merger.AddStreamSource(audioPath, MediaStreamType.Audio);
            merger.AddStreamSource(videoPath, MediaStreamType.Video);
            merger.OutputMimeType = videoStream.Format.Mime;
            merger.OutputShortName = videoStream.Format.Extension;
            merger.Build();
        }

        private async Task<string> DownloadMedia(GrabbedMedia media, IGrabResult grabResult)
        {
            lblProgress.Invoke(new Action(() => {
                lblProgress.Text = "Esperando...";
                prgDownload.Value = 0;
            }));

            using var response = await Client.GetAsync(media.ResourceUri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? -1L;
            using var downloadStream = await response.Content.ReadAsStreamAsync();
            //using var resourceStream = await grabResult.WrapStreamAsync(downloadStream);

            var downloadRate = 1048576;
            var path = Path.GetTempFileName();
            TempFiles.Add(path);
            var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, downloadRate, true);

            var read = 0;
            var totalRead = 0L;
            var buffer = new byte[downloadRate];
            CancellationToken token = new();

            lblProgress.Invoke(new Action(() => {
                lblProgress.Text = "Total: " + total;
            }));

            do
            {
                token.ThrowIfCancellationRequested();

                read = await downloadStream.ReadAsync(buffer, token);
                var data = new byte[read];
                buffer.ToList().CopyTo(0, data, 0, read);

                await fileStream.WriteAsync(buffer.AsMemory(0, read));

                // Update the percentage of file downloaded
                totalRead += read;

                lblProgress.Invoke(new Action(() =>
                {
                    var downloadPercentage = totalRead * 1d / (total * 1d) * 100;
                    lblProgress.Text = "Downloading " + media.Channels + " " + grabResult.Title + ": " + downloadPercentage.ToString("0.##") + "%" + " " + (totalRead / 1000000.0).ToString("0.##") + "MB";
                    prgDownload.Value = Convert.ToInt32(downloadPercentage);
                }));
            }
            while (read > 0);


            //using var fileStream = new FileStream(path, FileMode.Create);
            
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

            //for (var i = 0; i < resources.Count; i++)
            //{
            //    var resource = resources[i];
            //    Console.WriteLine($"{i}. {resource.Title ?? resource.FormatTitle ?? resource.Resolution}");
            //}

            //while (true)
            //{
            //    Console.Write($"Choose the {channel} file: ");
            //    var choiceStr = Console.ReadLine();
            //    if (!int.TryParse(choiceStr, out var choice))
            //    {
            //        Console.WriteLine("Number expected.");
            //        continue;
            //    }

            //    if (choice < 0 || choice >= resources.Count)
            //    {
            //        Console.WriteLine("Invalid number.");
            //        continue;
            //    }

            //    return resources[choice];
            //}
        }
    }
}