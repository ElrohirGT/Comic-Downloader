using System;
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

        public abstract Task DownloadComic(Uri url, string mainPath, HttpClient httpClient, SemaphoreSlim gate);

        public abstract Task<int> GetNumberOfImages(Uri url);

        protected async Task DownloadImageAsync(
            string directoryPath,
            Uri uri,
            object fileName,
            SemaphoreSlim gate,
            HttpClient httpClient)
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
                Console.WriteLine($"An error ocurred trying to download {uriWithoutQuery}");
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