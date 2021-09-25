namespace Comic_Downloader.CMD
{
    public struct DownloadReportEventArgs
    {
        /// <summary>
        /// The current count of images downloaded
        /// </summary>
        public int CurrentCount { get; set; }

        /// <summary>
        /// The total count of images that will be downloaded.
        /// </summary>
        public int TotalCount { get; set; }
    }
}