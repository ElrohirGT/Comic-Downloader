using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    /// <summary>
    /// Encapsulates all the methods required to download a comic from one online host.
    /// </summary>
    public interface IComicDownloader
    {
        /// <summary>
        /// An event that fires every time an image finished downloading.
        /// </summary>
        event Action ImageFinishedDownloading;

        /// <summary>
        /// Downloads a comic from the specified uri. This method needs to be thread safe.
        /// </summary>
        /// <param name="uri">The uri where the comic is.</param>
        /// <param name="mainPath">The local path where it will be saved.</param>
        /// <param name="httpClient">The client instance the application uses.</param>
        /// <param name="gate">A gate to not download all images at the same time.</param>
        /// <param name="errors">The collection that contains that'll contain all the errors.</param>
        /// <returns>A task that completes once the comic has been downloaded</returns>
        Task DownloadComic(Uri uri, string mainPath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors);

        /// <summary>
        /// Get's how many images the comic has. This method needs to be thread safe.
        /// </summary>
        /// <param name="uri">The uri where the comic is.</param>
        /// <param name="errors">The collection that contains that'll contain all the errors.</param>
        /// <returns>A task that gives the number of images the comic has.</returns>
        Task<int> GetNumberOfImages(Uri uri, BlockingCollection<string> errors);
    }
}