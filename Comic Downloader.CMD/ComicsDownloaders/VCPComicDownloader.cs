using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    internal class VCPComicDownloader : BaseComicDownloader
    {
        protected override async Task<int> Get_Number_Of_Images(Uri url)
        {
            var web = new HtmlWeb();
            HtmlDocument document = await web.LoadFromWebAsync(url.AbsoluteUri).ConfigureAwait(false);

            var imageNodes = document.DocumentNode.SelectNodes(@"//div[@class=""wp-content""]//img");
            return imageNodes.Count;
        }

        protected override async Task Download_Comic(Uri url, string mainPath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors)
        {
            var web = new HtmlWeb();
            HtmlDocument document = await web.LoadFromWebAsync(url.AbsoluteUri).ConfigureAwait(false);

            string title = document.DocumentNode.SelectSingleNode(@"//h1[@class=""titl""]").InnerText;
            string comicPath = SanitizeComicPath(Path.Combine(mainPath, title));

            var imageNodes = document.DocumentNode.SelectNodes(@"//div[@class=""wp-content""]//img");
            Task[] tasks = new Task[imageNodes.Count];
            for (int i = 0; i < imageNodes.Count; i++)
                tasks[i] = DownloadFileAsync(comicPath, new Uri(imageNodes[i].Attributes["src"].Value), gate, httpClient, errors, i.ToString());
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}