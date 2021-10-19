﻿using Comic_Downloader.CMD.ComicsUriProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    /// <summary>
    /// Basic Implementation of a <see cref="IDownloader"/>.
    /// </summary>
    public class Downloader : IDownloader
    {
        private readonly HttpClient _httpClient;//This should not be disposed of in this class
        private readonly object _lock = new();
        private readonly int _maxItems;
        private readonly IDictionary<string, IResourceUriProvider> _registeredProviders;
        private int _currentDownloadedImages;
        private int _totalImageCount;

        /// <summary>
        /// Creates an instance of <see cref="Downloader"/> with the default downlaoders.
        /// The default downloaders are:
        /// <see cref="VCPUriProvider"/>,
        /// <see cref="EHentaiOrgUriProvider"/>
        /// and <see cref="VMPUriProvider"/>.
        /// </summary>
        /// <param name="httpClient">The HTTP client to reuse.</param>
        /// <param name="maxItems">The maximum number of items that will be downloaded simultaneously.</param>
        public Downloader(HttpClient httpClient, int maxItems)
            : this(
                 httpClient,
                 maxItems: maxItems,
                 registeredProviders: new Dictionary<string, IResourceUriProvider>()
                 {
                     { "vercomicsporno.com", new VCPUriProvider() },
                     { "e-hentai.org", new EHentaiOrgUriProvider() },
                     { "vermangasporno.com", new VMPUriProvider() }
                 })
        { }

        /// <summary>
        /// Creates an instance of a <see cref="Downloader"/> with custom providers,
        /// the string is the host name and the value is the instance to reuse.
        /// An example of a host name would be "e-hentai.org".
        /// </summary>
        /// <param name="httpClient">The HTTP client to reuse.</param>
        /// <param name="registeredProviders">The custom providers to use.</param>
        /// <param name="maxItems">The maximum number of items that will be downloaded simultaneously.</param>
        public Downloader(HttpClient httpClient, IDictionary<string, IResourceUriProvider> registeredProviders, int maxItems = 1)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _registeredProviders = registeredProviders ?? throw new ArgumentNullException(nameof(registeredProviders));
            _maxItems = maxItems;
        }

        /// <summary>
        /// Event that fires every time an image is downloaded. Contains information about the current downloads.
        /// </summary>
        public event Action<DownloadReportEventArgs> DownloadReport;

        /// <summary>
        /// Downloads a bunch of comics from the urls,
        /// this method is provided for allowing the download of multiple comics at a time.
        /// </summary>
        /// <param name="urls">The uris where the comic images are located.</param>
        /// <param name="outputPath">The path where to download the comics.</param>
        /// <returns>An array of errors. If there were no errors it's an empty array.</returns>
        public async Task<string[]> DownloadComics(IEnumerable<Uri> urls, string outputPath)
        {
            using BlockingCollection<string> errors = new();
            _currentDownloadedImages = 0;
            _totalImageCount = 0;

            using BlockingCollection<IAsyncEnumerable<DownloadableFile>> fileEnumerables = new();
            await urls.ForEachParallelAsync(async (uri) =>
            {
                try
                {
                    fileEnumerables.Add(await GetImageUris(uri, outputPath).ConfigureAwait(false));
                }
                catch (Exception e)
                {
                    errors.Add($"An error ocurred downloading: {uri}\n{e.Message}");
                }
            }).ConfigureAwait(false);

            foreach (IAsyncEnumerable<DownloadableFile> fileEnumerable in fileEnumerables)
            {
                await fileEnumerable.ForEachParallelAsync(async (file) =>
                {
                    try
                    {
                        await DownloadFileAsync(file).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{e.Message}\n{e.InnerException?.Message}");
                    }
                }, _maxItems).ConfigureAwait(false);
            }
            return errors.ToArray();
        }

        private string ConstructFilePath(string uriWithoutQuery, object fileName, string comicPath)
        {
            fileName ??= Path.GetFileName(uriWithoutQuery);
            fileName = BaseResourceUriProvider.SanitizeFileName(fileName);
            string fileExtension = Path.GetExtension(uriWithoutQuery);

            return Path.Combine(comicPath, $"{fileName}{fileExtension}");
        }

        private async Task DownloadFileAsync(DownloadableFile downloadableFile)
        {
            //Sanitize directory path
            string uriWithoutQuery = downloadableFile.Uri.GetLeftPart(UriPartial.Path);
            string path = ConstructFilePath(uriWithoutQuery, downloadableFile.FileName, downloadableFile.OutputPath);

            try
            {
                Directory.CreateDirectory(downloadableFile.OutputPath);

                // Downloading the file via streams because it has better performance
                using var imageStream = await _httpClient.GetStreamAsync(downloadableFile.Uri).ConfigureAwait(false);
                using FileStream outputStream = File.Create(path);

                await imageStream.CopyToAsync(outputStream).ConfigureAwait(false);
                await outputStream.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new Exception($"An error ocurred trying to download {uriWithoutQuery}", e);
            }
            finally
            {
                OnFileDownloaded();
            }
        }

        private IResourceUriProvider GetDownloader(Uri url)
        {
            var host = url.Host;
            if (_registeredProviders.TryGetValue(host, out IResourceUriProvider downloader))
                return downloader;
            return null;
        }

        private async Task<IAsyncEnumerable<DownloadableFile>> GetImageUris(Uri uri, string mainPath)
        {
            IResourceUriProvider uriProvider = GetDownloader(uri);
            if (uriProvider is null)
                throw new NotSupportedException($"{uri} is not supported!");
            int numberOfImages = await uriProvider.GetNumberOfItems(uri).ConfigureAwait(false);
            Interlocked.Add(ref _totalImageCount, numberOfImages);

            return uriProvider.GetUris(uri, mainPath);
        }

        private void OnFileDownloaded()
        {
            lock (_lock)
            {
                DownloadReport?.Invoke(new DownloadReportEventArgs()
                {
                    CurrentCount = Interlocked.Increment(ref _currentDownloadedImages),
                    TotalCount = _totalImageCount
                });
            }
        }
    }
}