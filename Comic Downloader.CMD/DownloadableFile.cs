using System;

namespace Comic_Downloader.CMD
{
    public struct DownloadableFile
    {
        public object FileName { get; internal set; }
        public string OutputPath { get; internal set; }
        public Uri Uri { get; internal set; }
    }
}