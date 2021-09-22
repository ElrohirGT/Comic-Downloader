using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    internal abstract class BaseComicDownloader : IComicDownloader
    {
        public event Action ImageDownloaded;

        public abstract Task DownloadComic(Uri url, string mainPath, HttpClient httpClient, SemaphoreSlim gate);

        public abstract Task<int> GetNumberOfImages(Uri url);

        public async Task DownloadImageAsync(
            string directoryPath,
            Uri uri,
            object fileName,
            SemaphoreSlim gate,
            HttpClient httpClient)
        {
            await gate.WaitAsync().ConfigureAwait(false);

            var uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
            fileName ??= Path.GetFileName(uriWithoutQuery);
            var fileExtension = Path.GetExtension(uriWithoutQuery);

            var path = Path.Combine(directoryPath, $"{fileName}{fileExtension}");
            Directory.CreateDirectory(directoryPath);
            try
            {
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
                ImageDownloaded?.Invoke();
                gate.Release();
            }
        }
    }
}