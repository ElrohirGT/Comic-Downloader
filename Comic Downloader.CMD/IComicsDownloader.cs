using System;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    /// <summary>
    /// Represents a Comic Downloader.
    /// Encapsulates all the logic to download comics from multiple urls and saves the images to the specified path.
    /// </summary>
    public interface IComicsDownloader
    {
        /// <summary>
        /// Event that contains download information of the current process.
        /// </summary>
        event Action<DownloadReportEventArgs> DownloadReport;

        /// <summary>
        /// Downloads a comic from the specified <paramref name="url"/> to the specified <paramref name="mainPath"/>.
        /// </summary>
        /// <param name="url">The url where the images are located.</param>
        /// <param name="mainPath">The path where the comic folder will be created.</param>
        /// <returns>An array filled with all the errors. If there weren't any it's an empty array.</returns>
        Task<string[]> DownloadComic(Uri url, string mainPath);

        /// <summary>
        /// Downloads all the comics from the specified <paramref name="urls"/> if it recognizes them.
        /// </summary>
        /// <param name="urls">The array of urls of the comics.</param>
        /// <param name="outputPath">The path where the comics folders will be created.</param>
        /// <returns>An array filled with all the errors. If there weren't any it's an empty array.</returns>
        Task<string[]> DownloadComics(Uri[] urls, string outputPath);
    }
}