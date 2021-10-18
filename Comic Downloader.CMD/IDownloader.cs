using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    /// <summary>
    /// Represents a Comic Downloader.
    /// Encapsulates all the logic to download comics from multiple urls and saves the images to the specified path.
    /// </summary>
    public interface IDownloader
    {
        /// <summary>
        /// Event that contains download information of the current process.
        /// </summary>
        event Action<DownloadReportEventArgs> DownloadReport;

        /// <summary>
        /// Downloads all the comics from the specified <paramref name="urls"/> if it recognizes them.
        /// </summary>
        /// <param name="urls">The array of urls of the comics.</param>
        /// <param name="outputPath">The path where the comics folders will be created.</param>
        /// <returns>An array filled with all the errors. If there weren't any it's an empty array.</returns>
        Task<string[]> DownloadComics(IEnumerable<Uri> urls, string outputPath);
    }
}