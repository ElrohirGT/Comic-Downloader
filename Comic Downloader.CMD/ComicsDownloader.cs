using Comic_Downloader.CMD.ComicsDownloaders;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    internal class ComicsDownloader : IComicsDownloader, IDisposable
    {
        private Dictionary<string, IComicDownloader> _registeredDownloaders = new Dictionary<string, IComicDownloader>()
        {
            {
                "vercomicsporno.com",
                new VCPComicDownloader()
            },
            {
                "nhentai.net",
                new EHentaiOrgComicDownloader()
            }
        };

        private SemaphoreSlim _gate;
        private HttpClient _httpClient;//This should not be disposed of in this class
        private object _lock = new();
        private int _currentDownloadedImages;
        private int _totalImageCount;

        public event Action<DownloadReportEventArgs> DownloadReport;

        public ComicsDownloader(HttpClient httpClient, int maxImages)
        {
            _gate = new SemaphoreSlim(maxImages);
            _httpClient = httpClient;
            foreach (var downloader in _registeredDownloaders.Values)
                downloader.ImageDownloaded += OnImageDownloaded;
        }

        private IComicDownloader GetDownloader(Uri url)
        {
            var host = url.Host;
            if (_registeredDownloaders.TryGetValue(host, out IComicDownloader downloader))
                return downloader;
            return null;
        }

        /// <summary>
        /// Downloads a bunch of comics from the urls,
        /// this method is provided for allowing the download of multiple comics at a time.
        /// </summary>
        /// <param name="urls">The uris where the comic images are located.</param>
        /// <param name="outputPath">The path where to download the comics.</param>
        /// <returns>A task that finished once all comics have downloaded.</returns>
        public async Task DownloadComics(Uri[] urls, string outputPath)
        {
            _currentDownloadedImages = 0;
            _totalImageCount = 0;
            foreach (var url in urls)
            {
                IComicDownloader downloader = GetDownloader(url);
                if (downloader is null)
                    continue;
                _totalImageCount += await downloader.GetNumberOfImages(url);
            }
            List<Task> tasks = new List<Task>(urls.Length);
            for (int i = 0; i < urls.Length; i++)
            {
                Uri url = urls[i];
                IComicDownloader downloader = GetDownloader(url);
                if (downloader is null)
                    continue;
                tasks.Add(downloader.DownloadComic(url, outputPath, _httpClient, _gate));
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Downloads a comic from a url to the specified path,
        /// a folder with the comic name will be automatically generated if it doesn't exists already.
        /// This method is not thread-safe, so just call it one at a time.
        /// </summary>
        /// <param name="url">The uri where the images are located.</param>
        /// <param name="outputPath">The path were the comic folder will be created.</param>
        /// <returns>A task that completes once the comic has finished downloading.</returns>
        public async Task DownloadComic(Uri url, string outputPath)
        {
            IComicDownloader downloader = GetDownloader(url);
            if (downloader is null)
                throw new NotSupportedException("The specified uri is not supported yet.");

            _currentDownloadedImages = 0;
            _totalImageCount = await downloader.GetNumberOfImages(url);
            await downloader.DownloadComic(url, outputPath, _httpClient, _gate);
        }

        private void OnImageDownloaded()
        {
            lock (_lock)
            {
                DownloadReport?.Invoke(new DownloadReportEventArgs()
                {
                    CurrentCount = ++_currentDownloadedImages,
                    TotalCount = _totalImageCount
                });
            }
        }

        #region Implementing IDisposable

        ~ComicsDownloader()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                _gate.Dispose();
        }

        #endregion Implementing IDisposable
    }
}