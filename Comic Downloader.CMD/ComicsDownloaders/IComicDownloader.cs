using System;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    internal interface IComicDownloader
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
        /// <returns>A task that completes once the comic has been downloaded</returns>
        Task DownloadComic(Uri uri, string mainPath, System.Net.Http.HttpClient httpClient, SemaphoreSlim gate);
        /// <summary>
        /// Get's how many images the comic has. This method needs to be thread safe.
        /// </summary>
        /// <param name="uri">The uri where the comic is.</param>
        /// <returns>A task that gives the number of images the comic has.</returns>
        Task<int> GetNumberOfImages(Uri uri);
    }
}