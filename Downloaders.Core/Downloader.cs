using Downloaders.Core.UriProviders.ComicsUriProviders;
using Downloaders.Core.UriProviders.NewgroundsUriProviders;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Downloaders.Core
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
                     { "vermangasporno.com", new VMPUriProvider() },
                     { "www.newgrounds.com", new NewgroundsUriProvider() }
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

        ~Downloader()
        {
            Dispose(false);
        }

        /// <summary>
        /// Event that fires every time an image is downloaded. Contains information about the current downloads.
        /// </summary>
        public event Action<DownloadReportEventArgs>? DownloadReport;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<IDictionary<Uri, ICollection<string>>> DownloadFiles(IEnumerable<Uri> uris, string outputPath, Channel<DownloadableFile>? channel = null)
        {
            ConcurrentDictionary<Uri, ConcurrentBag<string>> errors = new();
            _currentDownloadedImages = 0;
            _totalImageCount = 0;

            UnboundedChannelOptions options = new() { SingleReader = true, SingleWriter = true };
            channel ??= Channel.CreateUnbounded<DownloadableFile>(options);

            var downloadFilesTask = Task.Run(
                () => channel.Reader.ReadAllAsync().ForEachParallelAsync(
                    async (file) =>
                        {
                            try
                            {
                                file.OutputPath ??= outputPath;
                                await DownloadFileAsync(file).ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                errors.TryAdd(file.PageUri, new ConcurrentBag<string>());
                                errors[file.PageUri].Add($"{e.Message}\n{e.InnerException?.Message}");
                            }
                        }, _maxItems
                    )
                );

            await uris.ForEachParallelAsync(async (uri) =>
            {
                try
                {
                    await GetItemsUris(uri, outputPath, channel.Writer).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    errors.TryAdd(uri, new ConcurrentBag<string>());
                    errors[uri].Add($"An error ocurred downloading: {uri}\n{e.Message}");
                }
            }).ConfigureAwait(false);

            channel.Writer.Complete();
            await downloadFilesTask.ConfigureAwait(false);
            //Casting each ConcurrentBag<string> into an IEnumerable<string>
            return new Dictionary<Uri, ICollection<string>>(errors.Select(pair=> KeyValuePair.Create<Uri, ICollection<string>>(pair.Key, pair.Value.ToArray())));
        }

        private static string ConstructFilePath(string uriWithoutQuery, object? fileName, string comicPath)
        {
            fileName ??= Path.GetFileName(uriWithoutQuery);
            fileName = BaseResourceUriProvider.SanitizeFileName(fileName);
            string fileExtension = Path.GetExtension(uriWithoutQuery);

            return Path.Combine(comicPath, $"{fileName}{fileExtension}");
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var item in _registeredProviders.Values)
                    item.Dispose();
            }
        }

        private async Task DownloadFileAsync(DownloadableFile downloadableFile)
        {
            //Sanitize directory path
            CancellationTokenSource cts = new(downloadableFile.TimeLimit);
            string uriWithoutQuery = downloadableFile.FileUri.GetLeftPart(UriPartial.Path);
            string path = ConstructFilePath(uriWithoutQuery, downloadableFile.FileName, downloadableFile.OutputPath);

            try
            {
                Directory.CreateDirectory(downloadableFile.OutputPath);

                //INFO: Downloading the file via streams because it has better performance with large files
                using var imageStream = await _httpClient.GetStreamAsync(downloadableFile.FileUri).ConfigureAwait(false);
                using FileStream outputStream = File.Create(path);

                var downloadTask = Task.Run(()=>
                {
                    imageStream.CopyTo(outputStream);
                    outputStream.Flush();
                });

                while (!downloadTask.IsCompleted)
                    cts.Token.ThrowIfCancellationRequested();

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

        private IResourceUriProvider? GetDownloader(Uri url)
        {
            var host = url.Host;
            return _registeredProviders.TryGetValue(host, out var downloader) ? downloader : null;
        }

        private async Task GetItemsUris(Uri uri, string mainPath, ChannelWriter<DownloadableFile> writer)
        {
            IResourceUriProvider? uriProvider = GetDownloader(uri);
            if (uriProvider is null)
                throw new NotSupportedException($"{uri.Host} is not supported!");
            int numberOfItems = await uriProvider.GetNumberOfItems(uri).ConfigureAwait(false);
            Interlocked.Add(ref _totalImageCount, numberOfItems);
            OnDownloadReport();

            await uriProvider.GetUris(uri, mainPath, writer).ConfigureAwait(false);
        }

        private void OnDownloadReport()
        {
            //INFO: locking is used so the events are invoked one after the other
            lock (_lock)
            {
                DownloadReport?.Invoke(new DownloadReportEventArgs()
                {
                    CurrentCount = _currentDownloadedImages,
                    TotalCount = _totalImageCount
                });
            }
        }

        private void OnFileDownloaded()
        {
            //INFO: Interlocked is used just in case a later refactoring uses this variable.
            Interlocked.Increment(ref _currentDownloadedImages);
            OnDownloadReport();
        }
    }
}