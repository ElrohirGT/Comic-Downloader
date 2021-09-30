using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    /// <summary>
    /// <see cref="IResourceDownloader"/> implementation for the <see href="vermangasporno.com"/> host.
    /// </summary>
    public class VMPComicDownloader : BaseComicDownloader
    {
        private HtmlWeb _web = new HtmlWeb();

        protected override async Task Download_Comic(Uri uri, string basePath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors)
        {
            HtmlDocument doc = await _web.LoadFromWebAsync(uri.AbsoluteUri);
            string comicTitle = doc.DocumentNode.SelectSingleNode(@"//div[@class=""comicimg""]//p[1]").InnerText;

            string comicPath = ConstructComicPath(basePath, comicTitle);
            var imageNodes = GetTheImages(doc);

            Task[] tasks = new Task[imageNodes.Length];
            for (int i = 0; i < imageNodes.Length; i++)
            {
                string imagePath = imageNodes[i].Attributes["src"].Value;
                Uri imageUri = new Uri(imagePath);
                tasks[i] = DownloadFileAsync(comicPath, imageUri, gate, httpClient, errors, i);
            }

            await Task.WhenAll(tasks);
        }

        protected override async Task<int> Get_Number_Of_Images(Uri url)
        {
            HtmlDocument doc = await _web.LoadFromWebAsync(url.AbsoluteUri);
            return GetTheImages(doc).Length;
        }

        private static HtmlNode[] GetTheImages(HtmlDocument doc)
        {
            var allImageNodes = doc.DocumentNode.SelectNodes(@"//div[@class=""comicimg""]//img");
            var imageNodes = allImageNodes.Where(n => n.Attributes["src"].Value.StartsWith("http")).ToArray();
            return imageNodes;
        }
    }
}