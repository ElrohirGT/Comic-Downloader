using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    /// <summary>
    /// Encapsulates all the methods required to download a resource from one online host.
    /// </summary>
    public interface IDownloader
    {
        /// <summary>
        /// An event that fires every time an item finished downloading.
        /// </summary>
        event Action ItemFinishedDownloading;

        /// <summary>
        /// Downloads a resource from the specified uri.
        /// The implementation has to be thread safe.
        /// </summary>
        /// <param name="uri">The uri where the resource is.</param>
        /// <param name="mainPath">The local path where the resource will be saved.</param>
        /// <param name="httpClient">The client instance the application uses.</param>
        /// <param name="gate">A gate to not download all items at the same time.</param>
        /// <param name="errors">The collection that'll contain all the errors.</param>
        /// <returns>A task that completes once the resource has been downloaded</returns>
        Task Download(Uri uri, string mainPath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors);

        /// <summary>
        /// Get's how many items will be downloaded.
        /// The implementation has to be thread safe.
        /// </summary>
        /// <param name="uri">The uri where of the resource.</param>
        /// <param name="errors">The collection that contains that'll contain all the errors.</param>
        /// <returns>A task that'll return the number of items that will be downloaded.</returns>
        Task<int> GetNumberOfItems(Uri uri, BlockingCollection<string> errors);
    }
}