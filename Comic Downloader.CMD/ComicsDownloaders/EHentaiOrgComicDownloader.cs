using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    internal class EHentaiOrgComicDownloader : BaseComicDownloader
    {
        private HtmlWeb _web = new HtmlWeb();

        public override async Task DownloadComic(Uri url, string mainPath, HttpClient httpClient, SemaphoreSlim gate)
        {
            HtmlDocument document = await _web.LoadFromWebAsync(url.AbsoluteUri).ConfigureAwait(false);

            string englishTitle = document.DocumentNode.SelectSingleNode(@"//h1[@id=""gn""]")?.InnerText;
            string japaneseTitle = document.DocumentNode.SelectSingleNode(@"//h1[@id=""gj""]")?.InnerText;
            string comicTitle = englishTitle ?? japaneseTitle;
            string directoryPath = Path.Combine(mainPath, comicTitle);

            HtmlNode tableNode = document.DocumentNode.SelectSingleNode(@"//table[@class=""ptt""]//td[last()-1]");
            int numberOfPages = int.Parse(tableNode.InnerText);
            List<Task> tasks = new();
            int imgCount = 0;
            for (int i = 0; i < numberOfPages; i++)
            {
                HtmlNodeCollection imgLinksNodes = document.DocumentNode.SelectNodes(@"//div[@id=""gdt""]//a");
                for (int j = 0; j < imgLinksNodes.Count; j++)
                {
                    HtmlDocument imgDoc = await _web.LoadFromWebAsync(imgLinksNodes[j].Attributes["href"].Value).ConfigureAwait(false);
                    var imgNode = imgDoc.DocumentNode.SelectSingleNode(@"//img[@id=""img""]");
                    Uri uri = new Uri(imgNode.Attributes["src"].Value);

                    tasks.Add(DownloadImageAsync(directoryPath, uri, imgCount + j, gate, httpClient));

                    bool isLastExecutionCycle = j + 1 == imgLinksNodes.Count;
                    if (isLastExecutionCycle)
                        imgCount += imgLinksNodes.Count;
                }

                bool lastExecutionCycle = i + 1 == numberOfPages;
                if (!lastExecutionCycle)
                {
                    HtmlNode nextPageNavigationNode = document.DocumentNode.SelectSingleNode(@"//table[@class=""ptt""]//td[last()]/a");
                    string nextPageUrl = nextPageNavigationNode.Attributes["href"].Value;

                    document = await _web.LoadFromWebAsync(nextPageUrl).ConfigureAwait(false);
                }
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public override async Task<int> GetNumberOfImages(Uri url)
        {
            //FIXME web.LoadFromWebAsync throws a null exception when this url is used.
            //https://e-hentai.org/g/913931/105605c620/
            HtmlDocument document = await _web.LoadFromWebAsync(url.AbsoluteUri).ConfigureAwait(false);

            var tdAdjecentNode = document.DocumentNode.SelectSingleNode(@"//td[@class=""gdt1""][text()=""Length:""]");

            string numberOfImages = tdAdjecentNode.NextSibling.InnerText.Split(' ')[0];
            return int.Parse(numberOfImages);
        }
    }
}