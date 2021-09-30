using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    /// <summary>
    /// The base for all resource downloaders, contains handy methods for downloading files.
    /// </summary>
    public abstract class BaseResourceDownloader : IResourceDownloader
    {
        private readonly Regex INVALID_CHARS_REGEX;
        private readonly string _errorDownloadingFormat;
        private readonly string _errorGettingNumberOfItemsFormat;

        protected BaseResourceDownloader(string errorDownloadingFormat, string errorGettingNumberOfItemsFormat)
        {
            _errorDownloadingFormat = errorDownloadingFormat;
            _errorGettingNumberOfItemsFormat = errorGettingNumberOfItemsFormat;

            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            INVALID_CHARS_REGEX = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
        }

        /// <summary>
        /// An event that fires every time an image has finished downloading.
        /// You shouldn't invoke this event directly, instead use <see cref="DownloadFileAsync(string, Uri, SemaphoreSlim, HttpClient, BlockingCollection{string}, object)"/>.
        /// </summary>
        public event Action ItemFinishedDownloading;

        public Task Download(Uri uri, string mainPath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors)
        {
            try
            {
                return _Download(uri, mainPath, httpClient, gate, errors);
            }
            catch (Exception e)
            {
                errors.Add(string.Format(_errorDownloadingFormat, uri.GetLeftPart(UriPartial.Path), e.Message));
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Override this method to start the download of the resource.
        /// All exceptions are automatically handled by <see cref="BaseResourceDownloader"/> so you don't need to use try/catch.
        /// </summary>
        /// <param name="uri">The uri where the resource is.</param>
        /// <param name="mainPath">The path where the output should be saved.</param>
        /// <param name="httpClient">The client to reuse</param>
        /// <param name="gate">The gate that controls how many concurrent downloads will be started.</param>
        /// <param name="errors">The collection that will have all the errors, should only be used to pass a reference <see cref="DownloadFileAsync(string, Uri, SemaphoreSlim, HttpClient, BlockingCollection{string}, object)"/>.</param>
        /// <returns>A task that completes once the resource was downloaded.</returns>
        protected abstract Task _Download(Uri uri, string mainPath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors);

        public Task<int> GetNumberOfItems(Uri uri, BlockingCollection<string> errors)
        {
            try
            {
                return Get_Number_Of_Items(uri);
            }
            catch (Exception e)
            {
                errors.Add(string.Format(_errorGettingNumberOfItemsFormat, uri.GetLeftPart(UriPartial.Path), e.Message));
                return Task.FromResult(0);
            }
        }

        protected abstract Task<int> Get_Number_Of_Items(Uri uri);

        /// <summary>
        /// Downloads a file asyncronously on the specified <paramref name="outputPath"/>.
        /// This path should be already sanitized, you can use the method <see cref="ConstructFilePath(string, object, string)"/>.
        /// You can also optionally rename the file that will be downloaded.
        /// <paramref name="outputPath"/> will be created if it doesn't exsits and
        /// <paramref name="fileName"/> will be sanitized of invalid chars for the operating system.
        /// </summary>
        /// <param name="outputPath">The path where the image will be downloaded. It must be sanitized.</param>
        /// <param name="uri">The uri where the image is online.</param>
        /// <param name="gate">The semaphore used to control how many downloads are active.</param>
        /// <param name="httpClient">The HTTP Client to use.</param>
        /// <param name="errors">The collection that stores the errors.</param>
        /// <param name="fileName">The name the file will have when it's downloaded. If this is null, the default name from the uri will be used.</param>
        /// <returns>A task that completes once the file is downloaded.</returns>
        protected async Task DownloadFileAsync(
            string outputPath,
            Uri uri,
            SemaphoreSlim gate,
            HttpClient httpClient,
            BlockingCollection<string> errors,
            object fileName = null)
        {
            await gate.WaitAsync().ConfigureAwait(false);

            //Sanitize directory path
            string uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
            string path = ConstructFilePath(uriWithoutQuery, fileName, outputPath);

            try
            {
                Directory.CreateDirectory(outputPath);

                // Downloading the file via streams because it has better performance
                using var imageStream = await httpClient.GetStreamAsync(uri).ConfigureAwait(false);
                using FileStream outputStream = File.Create(path);

                await imageStream.CopyToAsync(outputStream).ConfigureAwait(false);
                await outputStream.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                errors.Add($"An error ocurred trying to download {uriWithoutQuery}\n{e.Message}");
            }
            finally
            {
                ItemFinishedDownloading?.Invoke();
                gate.Release();
            }
        }

        private string ConstructFilePath(string uriWithoutQuery, object fileName, string comicPath)
        {
            fileName ??= Path.GetFileName(uriWithoutQuery);
            fileName = SanitizeFileName(fileName);
            string fileExtension = Path.GetExtension(uriWithoutQuery);

            return Path.Combine(comicPath, $"{fileName}{fileExtension}");
        }

        protected string SanitizeFileName(object fileName) => INVALID_CHARS_REGEX.Replace(fileName.ToString(), "");
    }
}