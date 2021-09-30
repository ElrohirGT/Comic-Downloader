using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    /// <summary>
    /// <see cref="IDownloader"/> implementation for the <see href="e-hentai.org"/> host.
    /// </summary>
    public class EHentaiOrgComicDownloader : BaseComicDownloader
    {
        private HtmlWeb _web = new HtmlWeb();

        protected override async Task Download_Comic(Uri url, string mainPath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors)
        {
            HtmlDocument document = await _web.LoadFromWebAsync(url.AbsoluteUri).ConfigureAwait(false);

            string englishTitle = document.DocumentNode.SelectSingleNode(@"//h1[@id=""gn""]")?.InnerText;
            string japaneseTitle = document.DocumentNode.SelectSingleNode(@"//h1[@id=""gj""]")?.InnerText;
            string comicTitle = englishTitle ?? japaneseTitle;
            string comicPath = ConstructComicPath(mainPath, comicTitle);

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

                    tasks.Add(DownloadFileAsync(comicPath, uri, gate, httpClient, errors, imgCount + j));

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

        protected override async Task<int> Get_Number_Of_Images(Uri url)
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