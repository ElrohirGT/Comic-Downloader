﻿using Comic_Downloader.CMD.ComicsDownloaders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    public class ComicsDownloader : IComicsDownloader, IDisposable
    {
        private int _currentDownloadedImages;
        private SemaphoreSlim _gate;
        private HttpClient _httpClient;//This should not be disposed of in this class
        private object _lock = new();

        private IDictionary<string, IComicDownloader> _registeredDownloaders;

        private int _totalImageCount;

        /// <summary>
        /// Creates an instance of <see cref="ComicsDownloader"/> with the default downlaoders.
        /// The default downloaders are <see cref="VCPComicDownloader"/> and <see cref="EHentaiOrgComicDownloader"/>.
        /// </summary>
        /// <param name="httpClient">The HTTP client to reuse.</param>
        /// <param name="maxImages">The maximum number of images that will be downloaded simultaneously.</param>
        public ComicsDownloader(HttpClient httpClient, int maxImages)
            : this(
                 httpClient,
                 maxImages: maxImages,
                 registeredDownloaders: new Dictionary<string, IComicDownloader>()
                 {
                    { "vercomicsporno.com", new VCPComicDownloader() },
                    { "e-hentai.org", new EHentaiOrgComicDownloader() }
                 })
        { }

        /// <summary>
        /// Creates an instance of a <see cref="ComicsDownloader"/> with custom downloaders,
        /// the string is the host name and the value is the instance to reuse.
        /// An example of a host name would be "e-hentai.org".
        /// </summary>
        /// <param name="httpClient">The HTTP client to reuse.</param>
        /// <param name="registeredDownloaders">The custom downloaders to use.</param>
        /// <param name="maxImages">The maximum number of images that will be downloaded simultaneously.</param>
        public ComicsDownloader(HttpClient httpClient, IDictionary<string, IComicDownloader> registeredDownloaders, int maxImages = 1)
        {
            _gate = new SemaphoreSlim(maxImages);
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _registeredDownloaders = registeredDownloaders ?? throw new ArgumentNullException(nameof(registeredDownloaders));

            foreach (var downloader in _registeredDownloaders.Values)
                downloader.ImageFinishedDownloading += OnImageDownloaded;
        }

        public event Action<DownloadReportEventArgs> DownloadReport;

        /// <summary>
        /// Downloads a comic from a url to the specified path,
        /// a folder with the comic name will be automatically generated if it doesn't exists already.
        /// This method is not thread-safe, so just call it one at a time.
        /// </summary>
        /// <param name="url">The uri where the images are located.</param>
        /// <param name="outputPath">The path were the comic folder will be created.</param>
        /// <returns>An array of errors. If there were no errors it's an empty array.</returns>
        public async Task<string[]> DownloadComic(Uri url, string outputPath)
        {
            IComicDownloader downloader = GetDownloader(url);
            if (downloader is null)
                throw new NotSupportedException("The specified uri is not supported yet.");
            using BlockingCollection<string> errors = new BlockingCollection<string>();

            _currentDownloadedImages = 0;
            _totalImageCount = await downloader.GetNumberOfImages(url, errors).ConfigureAwait(false);

            await downloader.DownloadComic(url, outputPath, _httpClient, _gate, errors).ConfigureAwait(false);
            return errors.ToArray();
        }

        /// <summary>
        /// Downloads a bunch of comics from the urls,
        /// this method is provided for allowing the download of multiple comics at a time.
        /// </summary>
        /// <param name="urls">The uris where the comic images are located.</param>
        /// <param name="outputPath">The path where to download the comics.</param>
        /// <returns>An array of errors. If there were no errors it's an empty array.</returns>
        public async Task<string[]> DownloadComics(Uri[] urls, string outputPath)
        {
            _currentDownloadedImages = 0;
            _totalImageCount = 0;
            using BlockingCollection<string> errors = new BlockingCollection<string>();

            foreach (var url in urls)
            {
                IComicDownloader downloader = GetDownloader(url);
                if (downloader is null)
                    continue;
                _totalImageCount += await downloader.GetNumberOfImages(url, errors).ConfigureAwait(false);
            }
            List<Task> tasks = new List<Task>(urls.Length);
            for (int i = 0; i < urls.Length; i++)
            {
                Uri url = urls[i];
                IComicDownloader downloader = GetDownloader(url);
                if (downloader is null)
                    continue;
                tasks.Add(downloader.DownloadComic(url, outputPath, _httpClient, _gate, errors));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
            return errors.ToArray();
        }

        private IComicDownloader GetDownloader(Uri url)
        {
            var host = url.Host;
            if (_registeredDownloaders.TryGetValue(host, out IComicDownloader downloader))
                return downloader;
            return null;
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