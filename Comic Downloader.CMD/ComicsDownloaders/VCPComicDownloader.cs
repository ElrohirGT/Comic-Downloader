using HtmlAgilityPack;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    internal class VCPComicDownloader : BaseComicDownloader
    {
        public override async Task<int> GetNumberOfImages(Uri url)
        {
            var web = new HtmlWeb();
            HtmlDocument document = await web.LoadFromWebAsync(url.AbsoluteUri).ConfigureAwait(false);

            var imageNodes = document.DocumentNode.SelectNodes(@"//div[@class=""wp-content""]//img");
            return imageNodes.Count;
        }

        public override async Task DownloadComic(Uri url, string mainPath, HttpClient httpClient, SemaphoreSlim gate)
        {
            var web = new HtmlWeb();
            HtmlDocument document = await web.LoadFromWebAsync(url.AbsoluteUri).ConfigureAwait(false);

            string title = document.DocumentNode.SelectSingleNode(@"//h1[@class=""titl""]").InnerText;
            string comicPath = Path.Combine(mainPath, title);

            var imageNodes = document.DocumentNode.SelectNodes(@"//div[@class=""wp-content""]//img");
            Task[] tasks = new Task[imageNodes.Count];
            for (int i = 0; i < imageNodes.Count; i++)
                tasks[i] = DownloadImageAsync(comicPath, new Uri(imageNodes[i].Attributes["src"].Value), i.ToString(), gate, httpClient);
            await Task.WhenAll(tasks);
        }
    }
}