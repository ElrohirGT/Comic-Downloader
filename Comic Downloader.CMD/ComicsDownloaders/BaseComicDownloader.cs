using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    internal abstract class BaseComicDownloader : IComicDownloader
    {
        private readonly Regex INVALID_CHARS_REGEX;

        protected BaseComicDownloader()
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            INVALID_CHARS_REGEX = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
        }

        public event Action ImageFinishedDownloading;

        public Task DownloadComic(Uri url, string mainPath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors)
        {
            try
            {
                return Download_Comic(url, mainPath, httpClient, gate, errors);
            }
            catch (Exception e)
            {
                errors.Add($"Error downloading comic: {url.GetLeftPart(UriPartial.Path)}\n{e.Message}");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Override this method to implement the action of downloading the comic.
        /// The <paramref name="errors"/> collection is passed, and is intended to be used only to pass a reference to the method
        /// <see cref="DownloadImageAsync(string, Uri, object, SemaphoreSlim, HttpClient, BlockingCollection{string})"/>
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

        public Task<int> GetNumberOfImages(Uri url, BlockingCollection<string> errors)
        {
            try
            {
                return Get_Number_Of_Images(url);
            }
            catch (Exception e)
            {
                errors.Add($"Error getting the number of images of: {url.GetLeftPart(UriPartial.Path)}\n{e.Message}");
                return Task.FromResult(0);
            }
        }

        protected abstract Task<int> Get_Number_Of_Images(Uri url);

        protected async Task DownloadImageAsync(
            string directoryPath,
            Uri uri,
            object fileName,
            SemaphoreSlim gate,
            HttpClient httpClient,
            BlockingCollection<string> errors)
        {
            await gate.WaitAsync().ConfigureAwait(false);

            //Sanitize directory path
            string uriWithoutQuery, path;
            SanitizePath(ref directoryPath, uri, ref fileName, out uriWithoutQuery, out path);

            try
            {
                Directory.CreateDirectory(directoryPath);

                // Downloading the file via streams because it has better performance
                using var imageStream = await httpClient.GetStreamAsync(uri).ConfigureAwait(false);
                using FileStream outputStream = new FileStream(path, FileMode.Create);

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

        private void SanitizePath(ref string directoryPath, Uri uri, ref object fileName, out string uriWithoutQuery, out string path)
        {
            string directoryName = Path.GetFileName(directoryPath);
            string parentDirectoryPath = Path.GetDirectoryName(directoryPath);
            directoryPath = Path.Combine(parentDirectoryPath, INVALID_CHARS_REGEX.Replace(directoryName, ""));

            uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
            fileName ??= Path.GetFileName(uriWithoutQuery);
            var fileExtension = Path.GetExtension(uriWithoutQuery);

            path = Path.Combine(directoryPath, $"{fileName}{fileExtension}");
        }
    }
}