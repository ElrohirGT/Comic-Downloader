using Comic_Downloader.CMD.ComicsUriProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Comic_Downloader.CMD
{
    /// <summary>
    /// Basic Implementation of a <see cref="IDownloader"/>.
    /// </summary>
    public class Downloader : IDownloader
    {
        private readonly HttpClient _httpClient;//This should not be disposed of in this class
        private readonly object _lock = new();
        private readonly IDictionary<string, IResourceUriProvider> _registeredProviders;
        private int _currentDownloadedImages;
        private int _totalImageCount;
        private int _maxItems;

        /// <summary>
        /// Creates an instance of <see cref="Downloader"/> with the default downlaoders.
        /// The default downloaders are:
        /// <see cref="VCPComicDownloader"/>,
        /// <see cref="EHentaiOrgComicDownloader"/>
        /// and <see cref="VMPComicDownloader"/>.
        /// </summary>
        /// <param name="httpClient">The HTTP client to reuse.</param>
        /// <param name="maxItems">The maximum number of items that will be downloaded simultaneously.</param>
        public Downloader(HttpClient httpClient, int maxItems)
            : this(
                 httpClient,
                 maxItems: maxItems,
                 registeredProviders: new Dictionary<string, IResourceUriProvider>()
                 {
                     { "vercomicsporno.com", new VCPComicDownloader() },
                     { "e-hentai.org", new EHentaiOrgComicDownloader() },
                     { "vermangasporno.com", new VMPComicDownloader() }
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

            ExecutionDataflowBlockOptions dataflowBlockOptions = new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = _maxItems };
            var uriToImagesUrisBlock = new TransformManyBlock<Uri, DownloadableFile>(
                async (uri) =>
                {
                    try
                    {
                        return await GetImageUris(uri, outputPath).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        errors.Add($"An error ocurred downloading: {uri}\n{e.Message}");
                        return Array.Empty<DownloadableFile>();
                    }
                }, dataflowBlockOptions);
            var downloaderBlock = new ActionBlock<DownloadableFile>(async (file) =>
            {
                try
                {
                    await DownloadFileAsync(file).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    errors.Add($"{e.Message}\n{e.InnerException?.Message}");
                }
            }, dataflowBlockOptions);

            using IDisposable imagesUriToDownloaderLink = uriToImagesUrisBlock.LinkTo(downloaderBlock, new DataflowLinkOptions() { PropagateCompletion = true });

            List<Task> tasks = new();
            foreach (var uri in urls)
                tasks.Add(uriToImagesUrisBlock.SendAsync(uri));
            await Task.WhenAll(tasks).ConfigureAwait(false);
            uriToImagesUrisBlock.Complete();

            await downloaderBlock.Completion.ConfigureAwait(false);
            return errors.ToArray();
        }

        private async Task<IEnumerable<DownloadableFile>> GetImageUris(Uri uri, string mainPath)
        {
            IResourceUriProvider uriProvider = GetDownloader(uri);
            if (uriProvider is null)
                throw new NotSupportedException($"{uri} is not supported!");
            int numberOfImages = await uriProvider.GetNumberOfItems(uri).ConfigureAwait(false);
            Interlocked.Add(ref _totalImageCount, numberOfImages);

            return await uriProvider.GetUris(uri, mainPath).ConfigureAwait(false);
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
                OnImageDownloaded();
            }
        }

        private string ConstructFilePath(string uriWithoutQuery, object fileName, string comicPath)
        {
            fileName ??= Path.GetFileName(uriWithoutQuery);
            fileName = BaseResourceUriProvider.SanitizeFileName(fileName);
            string fileExtension = Path.GetExtension(uriWithoutQuery);

            return Path.Combine(comicPath, $"{fileName}{fileExtension}");
        }

        private IResourceUriProvider GetDownloader(Uri url)
        {
            var host = url.Host;
            if (_registeredProviders.TryGetValue(host, out IResourceUriProvider downloader))
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
    }
}