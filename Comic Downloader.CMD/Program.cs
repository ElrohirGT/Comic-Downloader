using HtmlAgilityPack;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    class Program
    {
        private const int MAX_IMAGES_AT_A_TIME = 15;
        private static HttpClient _httpClient = new HttpClient();
        static SemaphoreSlim _gate = new SemaphoreSlim(MAX_IMAGES_AT_A_TIME);
        static int _currentImageCount = 0;
        static int _maxImageCount = 0;

        static void Main()
        {
            //TODO: Major Refactoring needed, this is just the basic functionality done.
            Console.Clear();
            Console.Write("Link: ");
            string url = Console.ReadLine();
            Console.Write("Done Path: ");
            string mainPath = Console.ReadLine();
            //string url = "https://vercomicsporno.com/incognitymous-cataratas-lujuriosas-2";
            //string mainPath = @"D:\elroh\Documents\TestsDownloads";

            var web = new HtmlWeb();
            HtmlDocument document = web.Load(url);

            string title = document.DocumentNode.SelectSingleNode(@"//h1[@class=""titl""]").InnerText;
            string comicPath = Path.Combine(mainPath, title);

            var imageNodes = document.DocumentNode.SelectNodes(@"//div[@class=""wp-content""]//img");
            _maxImageCount = imageNodes.Count;

            Task[] tasks = new Task[_maxImageCount];
            for (int i = 0; i < _maxImageCount; i++)
                tasks[i] = DownloadImageAsync(comicPath, new Uri(imageNodes[i].Attributes["src"].Value), i.ToString());
            Task.WaitAll(tasks);
        }
        /// <summary>
        /// Downloads an image asynchronously from the <paramref name="uri"/> and places it in the specified <paramref name="directoryPath"/> with the specified <paramref name="fileName"/>.
        /// </summary>
        /// <param name="directoryPath">The relative or absolute path to the directory to place the image in.</param>
        /// <param name="uri">The URI for the image to download.</param>
        /// <param name="fileName">The name of the file without the file extension.</param>
        public static async Task DownloadImageAsync(string directoryPath, Uri uri, object fileName = null)
        {
            await _gate.WaitAsync();
            var uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
            fileName ??= Path.GetFileName(uriWithoutQuery);
            var fileExtension = Path.GetExtension(uriWithoutQuery);

            var path = Path.Combine(directoryPath, $"{fileName}{fileExtension}");
            Directory.CreateDirectory(directoryPath);
            try
            {
                // Downloading the file via streams because it has better performance
                var imageStream = await _httpClient.GetStreamAsync(uri).ConfigureAwait(false);
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
                Interlocked.Increment(ref _currentImageCount);
                _gate.Release();
            }
        }
    }
}
