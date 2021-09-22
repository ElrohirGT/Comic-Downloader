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
            string regexSearch = new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            directoryPath = r.Replace(directoryPath, "");

            var uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
            fileName ??= Path.GetFileName(uriWithoutQuery);
            var fileExtension = Path.GetExtension(uriWithoutQuery);

            var path = Path.Combine(directoryPath, $"{fileName}{fileExtension}");
            try
            {
                Directory.CreateDirectory(directoryPath);

                // Downloading the file via streams because it has better performance
                var imageStream = await httpClient.GetStreamAsync(uri).ConfigureAwait(false);
                using (FileStream outputStream = new FileStream(path, FileMode.Create))
                {
                    await imageStream.CopyToAsync(outputStream).ConfigureAwait(false);
                    await outputStream.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"An error ocurred trying to download {uriWithoutQuery}");
            }
            finally
            {
                ImageFinishedDownloading?.Invoke();
                gate.Release();
            }
        }
    }
}