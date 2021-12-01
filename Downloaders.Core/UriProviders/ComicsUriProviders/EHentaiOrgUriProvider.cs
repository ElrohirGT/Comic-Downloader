using HtmlAgilityPack;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.ComicsUriProviders
{
    /// <summary>
    /// <see cref="IResourceUriProvider"/> implementation for the <see href="e-hentai.org"/> host.
    /// </summary>
    public sealed class EHentaiOrgUriProvider : BaseComicUriProvider
    {
        private readonly HtmlWeb _web = new();
        private readonly TimeSpan FILE_TIME_LIMIT = TimeSpan.FromMinutes(1.5);

        public override async Task<int> GetNumberOfItems(Uri uri)
        {
            //FIXME web.LoadFromWebAsync throws a null exception when this url is used.
            //https://e-hentai.org/g/913931/105605c620/
            HtmlDocument document = await _web.LoadFromWebAsync(uri.AbsoluteUri).ConfigureAwait(false);

            var tdAdjecentNode = document.DocumentNode.SelectSingleNode(@"//td[@class=""gdt1""][text()=""Length:""]");

            string numberOfImages = tdAdjecentNode.NextSibling.InnerText.Split(' ')[0];
            return int.Parse(numberOfImages);
        }

        public override async Task GetUris(Uri uri, string mainPath, ChannelWriter<DownloadableFile> writer)
        {
            HtmlDocument document = await _web.LoadFromWebAsync(uri.AbsoluteUri).ConfigureAwait(false);

            string comicTitle = document.DocumentNode.SelectSingleNode(@"//h1[@id=""gn""]").InnerText;
            string comicPath = ConstructComicPath(mainPath, comicTitle);

            HtmlNode tableNode = document.DocumentNode.SelectSingleNode(@"//table[@class=""ptt""]//td[last()-1]");
            int numberOfPages = int.Parse(tableNode.InnerText);

            int imageCount = 0;
            for (int i = 0; i < numberOfPages; i++)
            {
                HtmlNodeCollection imgLinksNodes = document.DocumentNode.SelectNodes(@"//div[@id=""gdt""]//a");

                //INFO: This way all image links from a page will be released at the same time.
                await imgLinksNodes.ForParallelAsync(async (int index, HtmlNode imgLinkNode) =>
                {
                    int filename = imageCount + index;
                    HtmlDocument imgDoc = await _web.LoadFromWebAsync(imgLinkNode.Attributes["href"].Value).ConfigureAwait(false);

                    var imgNode = imgDoc.DocumentNode.SelectSingleNode(@"//img[@id=""img""]");
                    Uri imageUri = new(imgNode.Attributes["src"].Value);

                    DownloadableFile file = new() { FileName = filename, OutputPath = comicPath, FileUri = imageUri, PageUri = uri, TimeLimit = FILE_TIME_LIMIT };
                    await writer.WriteAsync(file).ConfigureAwait(false);
                }).ConfigureAwait(false);
                imageCount += imgLinksNodes.Count;

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