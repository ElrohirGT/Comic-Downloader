using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Downloaders.Core
{
    /// <summary>
    /// Encapsulates all the methods required to download a resource from one online host.
    /// </summary>
    public interface IResourceUriProvider : IDisposable
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
        /// <param name="mainPath">The main output path, may be used to construct other paths were the <see cref="DownloadableFile"/> will be downloaded.</param>
        /// <param name="writer">The channel that stores all the <see cref="DownloadableFile"/>'s.</param>
        /// <returns>A task that completes once all uris are written to the <paramref name="writer"/>.</returns>
        Task GetUris(Uri uri, string mainPath, ChannelWriter<DownloadableFile> writer);
    }
}