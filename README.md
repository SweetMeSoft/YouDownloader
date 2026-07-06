# YouDownloader

[![C#](https://img.shields.io/badge/C%23-10.0-239120.svg?style=flat&logo=c-sharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg?style=flat&logo=.net&logoColor=white)](https://dotnet.microsoft.com/download)
[![WPF](https://img.shields.io/badge/WPF-Desktop-blue.svg?style=flat)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)

## Table of Contents
- [Project Summary](#project-summary)
- [Functionalities](#functionalities)
- [Libraries and Dependencies](#libraries-and-dependencies)
- [Core Implementation](#core-implementation)
- [Versions](#versions)
- [Folder Structure](#folder-structure)
- [Design Patterns](#design-patterns)
- [Configurations](#configurations)
- [Integrations](#integrations)

## Project Summary
YouDownloader is a desktop-based utility application designed to facilitate the downloading, conversion, and packaging of media content from online video platforms. Its business purpose is to provide a clean, local application interface for queueing media downloads, extracting closed captions, and merging video and audio streams into single containers, targeting end users who require offline availability of media.

## Functionalities
- Multi-threaded queuing of media downloads.
- Extraction and download of audio-only streams at high bitrate.
- Extraction and download of video-only streams at high quality.
- Automatic multiplexing of video and audio streams into unified files.
- Retrieval and generation of closed caption subtitle tracks.
- Automated creation of text transcript summaries from closed captions.
- Dynamic visual progress reporting including size, downloaded bytes, and speed.

## Libraries and Dependencies

| Library Name | Version | Purpose |
| :--- | :--- | :--- |
| YoutubeExplode | 6.6.0 | Interfaces with online video services to extract metadata, stream info, and retrieve content streams. |
| YoutubeExplode.Converter | 6.6.0 | Handles the download and conversion pipelines of raw streams retrieved from the YouTube platform. |
| SharpGrabber.Converter | 1.1.0 | Manages media merging and conversion processes using standard system codecs. |
| Syroot.Windows.IO.KnownFolders | 1.3.0 | Resolves standard Windows OS folder paths (such as the Downloads directory) dynamically. |
| System.Linq.Async.Queryable | 7.0.1 | Provides asynchronous LINQ query capabilities for non-blocking collection transformations. |

## Core Implementation
The application compiles into a Windows-native desktop executable relying on the Windows Presentation Foundation (WPF) UI framework. The download pipeline uses standard HTTP clients to retrieve fragmented audio and video streams asynchronously. Merging operations utilize pre-compiled FFmpeg binary codecs packaged within the application directory to perform low-overhead, serverless multiplexing of streams on the user's host CPU.

## Versions
- Target SDK: .NET 10.0 (`net10.0-windows`)
- WPF Framework Version: 10.0
- External Codec Suite: FFmpeg v6.1

## Folder Structure
```
YouDownloader/
├── YouDownloader/
│   ├── ffmpeg61/                   # Local binary dependencies for media processing
│   ├── Models/
│   │   └── VideoInfo.cs            # Data representation for download queue items
│   ├── App.xaml                    # Application life-cycle and entry configuration
│   ├── InlineProgress.cs           # Thread-safe UI progress reporter wrapper
│   └── MainWindow.xaml             # User interface layout and code-behind handlers
├── YouDownloader.Setup/
│   └── YouDownloader.Setup.vdproj  # Visual Studio Installer Project configuration
└── YouDownloader.sln               # Solution configuration file
```

## Design Patterns
The application utilizes the code-behind layout architecture standard in WPF desktop developments. The UI layer in `MainWindow.xaml` binds directly to an `ObservableCollection` representing the download queue, while asynchronous event handlers execute operations off the main thread. Thread marshaling is achieved using control Dispatchers to update elements safely without violating cross-thread runtime constraints.

## Configurations
- Environment: Requires Microsoft .NET Desktop Runtime 10.0 or later on a Windows host.
- Project Build Configuration: Configuration parameters are managed in the MSBuild-compatible project file (`YouDownloader.csproj`). Output builds compile directly into standard target folders (e.g., `bin/Debug/net10.0-windows`).

## Integrations
- YouTube Platform: Performs queries against the YouTube servers to download streams and closed captions.
- Local System: Interfaces directly with the Windows shell system to resolve host folders (Downloads directory) and access the local file system.
