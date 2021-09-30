using Comic_Downloader.CMD.ComicsDownloaders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    /// <summary>
    /// Basic Implementation of a <see cref="IComicsDownloader"/>.
    /// </summary>
    public class Downloader : IComicsDownloader, IDisposable
    {
        private readonly SemaphoreSlim _gate;
        private readonly HttpClient _httpClient;//This should not be disposed of in this class
        private readonly object _lock = new();
        private readonly IDictionary<string, IDownloader> _registeredDownloaders;
        private int _currentDownloadedImages;
        private int _totalImageCount;

        /// <summary>
        /// Creates an instance of <see cref="Downloader"/> with the default downlaoders.
        /// The default downloaders are:
        /// <see cref="VCPComicDownloader"/>,
        /// <see cref="EHentaiOrgComicDownloader"/>
        /// and <see cref="VMPComicDownloader"/>.
        /// </summary>
        /// <param name="httpClient">The HTTP client to reuse.</param>
        /// <param name="maxImages">The maximum number of images that will be downloaded simultaneously.</param>
        public Downloader(HttpClient httpClient, int maxImages)
            : this(
                 httpClient,
                 maxImages: maxImages,
                 registeredDownloaders: new Dictionary<string, IDownloader>()
                 {
                     { "vercomicsporno.com", new VCPComicDownloader() },
                     { "e-hentai.org", new EHentaiOrgComicDownloader() },
                     {"vermangasporno.com", new VMPComicDownloader() }
                 })
        { }

        /// <summary>
        /// Creates an instance of a <see cref="Downloader"/> with custom downloaders,
        /// the string is the host name and the value is the instance to reuse.
        /// An example of a host name would be "e-hentai.org".
        /// </summary>
        /// <param name="httpClient">The HTTP client to reuse.</param>
        /// <param name="registeredDownloaders">The custom downloaders to use.</param>
        /// <param name="maxImages">The maximum number of images that will be downloaded simultaneously.</param>
        public Downloader(HttpClient httpClient, IDictionary<string, IDownloader> registeredDownloaders, int maxImages = 1)
        {
            _gate = new SemaphoreSlim(maxImages);
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _registeredDownloaders = registeredDownloaders ?? throw new ArgumentNullException(nameof(registeredDownloaders));

            foreach (var downloader in _registeredDownloaders.Values)
                downloader.ItemFinishedDownloading += OnImageDownloaded;
        }

        /// <summary>
        /// Event that fires every time an image is downloaded. Contains information about the current downloads.
        /// </summary>
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
            IDownloader downloader = GetDownloader(url);
            if (downloader is null)
                throw new NotSupportedException("The specified uri is not supported yet.");
            using BlockingCollection<string> errors = new BlockingCollection<string>();

            _currentDownloadedImages = 0;
            _totalImageCount = await downloader.GetNumberOfItems(url, errors).ConfigureAwait(false);

            await downloader.Download(url, outputPath, _httpClient, _gate, errors).ConfigureAwait(false);
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
                IDownloader downloader = GetDownloader(url);
                if (downloader is null)
                    continue;
                _totalImageCount += await downloader.GetNumberOfItems(url, errors).ConfigureAwait(false);
            }
            List<Task> tasks = new List<Task>(urls.Length);
            for (int i = 0; i < urls.Length; i++)
            {
                Uri url = urls[i];
                IDownloader downloader = GetDownloader(url);
                if (downloader is null)
                    continue;
                tasks.Add(downloader.Download(url, outputPath, _httpClient, _gate, errors));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
            return errors.ToArray();
        }

        private IDownloader GetDownloader(Uri url)
        {
            var host = url.Host;
            if (_registeredDownloaders.TryGetValue(host, out IDownloader downloader))
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

        ~Downloader()
        {
            Dispose(false);
        }

        /// <summary>
        /// Frees all managed resources from this instance of <see cref="Downloader"/>.
        /// It also unsubscribes from all <see cref="IDownloader.ItemFinishedDownloading"/> events.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                _gate.Dispose();

            foreach (var downloader in _registeredDownloaders.Values)
                downloader.ItemFinishedDownloading -= OnImageDownloaded;
        }

        #endregion Implementing IDisposable
    }
}