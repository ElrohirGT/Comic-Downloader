using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsUriProviders
{
    /// <summary>
    /// <see cref="IResourceUriProvider"/> implementation for the <see href="e-hentai.org"/> host.
    /// </summary>
    public sealed class EHentaiOrgComicDownloader : BaseResourceUriProvider
    {
        private HtmlWeb _web = new HtmlWeb();

        public override async Task<int> GetNumberOfItems(Uri uri)
        {
            //FIXME web.LoadFromWebAsync throws a null exception when this url is used.
            //https://e-hentai.org/g/913931/105605c620/
            HtmlDocument document = await _web.LoadFromWebAsync(uri.AbsoluteUri).ConfigureAwait(false);

            var tdAdjecentNode = document.DocumentNode.SelectSingleNode(@"//td[@class=""gdt1""][text()=""Length:""]");

            string numberOfImages = tdAdjecentNode.NextSibling.InnerText.Split(' ')[0];
            return int.Parse(numberOfImages);
        }

        public override async IAsyncEnumerable<DownloadableFile> GetUris(Uri uri, string mainPath)
        {
            HtmlDocument document = await _web.LoadFromWebAsync(uri.AbsoluteUri).ConfigureAwait(false);

            string englishTitle = document.DocumentNode.SelectSingleNode(@"//h1[@id=""gn""]")?.InnerText;
            string japaneseTitle = document.DocumentNode.SelectSingleNode(@"//h1[@id=""gj""]")?.InnerText;
            string comicTitle = englishTitle ?? japaneseTitle;
            string comicPath = ConstructComicPath(mainPath, comicTitle);

            HtmlNode tableNode = document.DocumentNode.SelectSingleNode(@"//table[@class=""ptt""]//td[last()-1]");
            int numberOfPages = int.Parse(tableNode.InnerText);

            int imgCount = 0;
            for (int i = 0; i < numberOfPages; i++)
            {
                HtmlNodeCollection imgLinksNodes = document.DocumentNode.SelectNodes(@"//div[@id=""gdt""]//a");
                for (int j = 0; j < imgLinksNodes.Count; j++)
                {
                    HtmlDocument imgDoc = await _web.LoadFromWebAsync(imgLinksNodes[j].Attributes["href"].Value).ConfigureAwait(false);
                    var imgNode = imgDoc.DocumentNode.SelectSingleNode(@"//img[@id=""img""]");
                    Uri imageUri = new Uri(imgNode.Attributes["src"].Value);

                    yield return new DownloadableFile() { FileName = imgCount + j, OutputPath = comicPath, Uri = imageUri };

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
        }
    }
}