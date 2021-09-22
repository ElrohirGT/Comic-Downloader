using System;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    internal interface IComicDownloader
    {
        event Action ImageDownloaded;

        Task DownloadComic(Uri url, string mainPath, System.Net.Http.HttpClient httpClient, SemaphoreSlim gate);

        Task<int> GetNumberOfImages(Uri url);
    }
}