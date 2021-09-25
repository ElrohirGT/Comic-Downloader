using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    /// <summary>
    /// Base implementation of <see cref="IComicDownloader"/>.
    /// Contains helper methods that help subclasses implement the methos of <see cref="IComicDownloader"/>.
    /// </summary>
    public abstract class BaseComicDownloader : IComicDownloader
    {
        private readonly Regex INVALID_CHARS_REGEX;

        protected BaseComicDownloader()
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            INVALID_CHARS_REGEX = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
        }

        /// <summary>
        /// An event that fires every time an image has finished downloading.
        /// You shouldn't invoke this event directly, instead use <see cref="DownloadFileAsync(string, Uri, SemaphoreSlim, HttpClient, BlockingCollection{string}, object)"/>.
        /// </summary>
        public event Action ImageFinishedDownloading;

        public Task DownloadComic(Uri uri, string mainPath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors)
        {
            try
            {
                return Download_Comic(uri, mainPath, httpClient, gate, errors);
            }
            catch (Exception e)
            {
                errors.Add($"Error downloading comic: {uri.GetLeftPart(UriPartial.Path)}\n{e.Message}");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Override this method to implement the action of downloading the comic.
        /// The <paramref name="errors"/> collection is passed, and is intended to be used only to pass a reference to the method
        /// <see cref="DownloadFileAsync(string, Uri, SemaphoreSlim, HttpClient, BlockingCollection{string}, object)"/>.
        /// Any exception that is thrown in this method will be correctly handed by <see cref="BaseComicDownloader"/>,
        /// so you don't need to try/catch this method.
        /// </summary>
        /// <param name="uri">The uri where the comic is located.</param>
        /// <param name="basePath">The path where the comic will be downloaded.</param>
        /// <param name="httpClient">The client to use.</param>
        /// <param name="gate">This allows to have a control on how many images are downloaded at a time.</param>
        /// <param name="errors">The errors collection</param>
        /// <returns>
        /// A task that completes once the comic has been downloaded or an error ocurred downloading the comic.
        /// It doesn't stops for image errors.
        /// </returns>
        protected abstract Task Download_Comic(Uri uri, string basePath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors);

        public Task<int> GetNumberOfImages(Uri uri, BlockingCollection<string> errors)
        {
            try
            {
                return Get_Number_Of_Images(uri);
            }
            catch (Exception e)
            {
                errors.Add($"Error getting the number of images of: {uri.GetLeftPart(UriPartial.Path)}\n{e.Message}");
                return Task.FromResult(0);
            }
        }

        /// <summary>
        /// Override this method to implement the action of getting the number of images.
        /// You don't need to try/catch this method, <see cref="BaseComicDownloader"/> already handles any exception this method could throw.
        /// </summary>
        /// <param name="url">The url where the comic resides.</param>
        /// <returns>The number of images the comic has.</returns>
        protected abstract Task<int> Get_Number_Of_Images(Uri url);

        /// <summary>
        /// Creates a sanitizes a comic path.
        /// </summary>
        /// <param name="basePath">The path where the comic should be downloaded.</param>
        /// <param name="comicTitle">The title of the comic.</param>
        /// <returns>The sanitized comic path.</returns>
        protected string ConstructComicPath(string basePath, string comicTitle)
            => Path.Combine(basePath, INVALID_CHARS_REGEX.Replace(comicTitle.Trim(), ""));

        /// <summary>
        /// Downloads an image asyncronously on the specified <paramref name="comicPath"/>.
        /// This path should be already sanitized, you can use the method <see cref="ConstructComicPath(string, string)"/>.
        /// You can also optionally rename the file that will be downloaded.
        /// </summary>
        /// <param name="comicPath">The path where the image will be downloaded. It must be sanitized.</param>
        /// <param name="uri">The uri where the image is online.</param>
        /// <param name="gate">The semaphore used to control how many downloads are active.</param>
        /// <param name="httpClient">The HTTP Client to use.</param>
        /// <param name="errors">The collection that stores the errors.</param>
        /// <param name="fileName">The name the file will have when it's downloaded. If this is null, the default name from the uri will be used.</param>
        /// <returns>A task that completes once the file is downloaded.</returns>
        protected async Task DownloadFileAsync(
            string comicPath,
            Uri uri,
            SemaphoreSlim gate,
            HttpClient httpClient,
            BlockingCollection<string> errors,
            object fileName = null)
        {
            await gate.WaitAsync().ConfigureAwait(false);

            //Sanitize directory path
            string uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
            string path = ConstructImagePath(uriWithoutQuery, fileName, comicPath);

            try
            {
                Directory.CreateDirectory(comicPath);

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
                ImageFinishedDownloading?.Invoke();
                gate.Release();
            }
        }

        private string ConstructImagePath(string uriWithoutQuery, object fileName, string comicPath)
        {
            fileName ??= Path.GetFileName(uriWithoutQuery);
            fileName = SanitizeFileName(fileName);
            string fileExtension = Path.GetExtension(uriWithoutQuery);

            return Path.Combine(comicPath, $"{fileName}{fileExtension}");
        }

        private object SanitizeFileName(object fileName) => INVALID_CHARS_REGEX.Replace(fileName.ToString(), "");
    }
}