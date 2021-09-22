namespace Comic_Downloader.CMD
{
    public struct DownloadReportEventArgs
    {
        public int CurrentCount { get; set; }
        public int TotalCount { get; set; }
    }
}