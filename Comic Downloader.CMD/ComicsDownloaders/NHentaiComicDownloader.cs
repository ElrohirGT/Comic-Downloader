using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    internal class NHentaiComicDownloader : BaseComicDownloader
    {
        public override Task DownloadComic(Uri url, string mainPath, HttpClient httpClient, SemaphoreSlim gate)
        {
            throw new NotImplementedException();
        }

        public override Task<int> GetNumberOfImages(Uri url)
        {
            throw new NotImplementedException();
        }
    }
}