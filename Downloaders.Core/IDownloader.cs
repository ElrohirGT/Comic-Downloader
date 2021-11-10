using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Downloaders.Core
{
    /// <summary>
    /// Represents a Downloader.
    /// Encapsulates all the logic to download files from multiple urls and saves them to the specified path.
    /// </summary>
    public interface IDownloader : IDisposable
    {
        /// <summary>
        /// Event that contains download information of the current process.
        /// </summary>
        event Action<DownloadReportEventArgs> DownloadReport;

        /// <summary>
        /// Downloads all the files from the specified <paramref name="uris"/> if it recognizes them.
        /// </summary>
        /// <param name="uris">The array of urls of the files.</param>
        /// <param name="outputPath">The path where the files will be downloaded.</param>
        /// <param name="channel">The channel that will serve as a communication device between threads.</param>
        /// <returns>A dictionary with the errors encountered for each uri.</returns>
        Task<IDictionary<Uri, ICollection<string>>> DownloadFiles(IEnumerable<Uri> uris, string outputPath, Channel<DownloadableFile>? channel = null);
    }
}