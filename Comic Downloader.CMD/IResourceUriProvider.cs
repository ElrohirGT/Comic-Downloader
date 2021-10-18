using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    /// <summary>
    /// Encapsulates all the methods required to download a resource from one online host.
    /// </summary>
    public interface IResourceUriProvider
    {
        /// <summary>
        /// Get's how many items will be downloaded.
        /// The implementation needs to be thread safe.
        /// </summary>
        /// <param name="uri">The uri where the resource lives.</param>
        /// <returns>A task that'll return the number of items that will be downloaded.</returns>
        Task<int> GetNumberOfItems(Uri uri);

        /// <summary>
        /// Get's all the files that will be downloaded.
        /// The implementation needs to be thread safe.
        /// </summary>
        /// <param name="uri">The uri where the resource lives.</param>
        /// <returns>An async enumerator that'll return all the uris.</returns>
        IAsyncEnumerable<DownloadableFile> GetUris(Uri uri, string mainPath);
    }
}