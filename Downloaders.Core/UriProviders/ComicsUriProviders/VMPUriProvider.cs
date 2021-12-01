using HtmlAgilityPack;
using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.ComicsUriProviders
{
    /// <summary>
    /// <see cref="IResourceUriProvider"/> implementation for the <see href="vermangasporno.com"/> host.
    /// </summary>
    public sealed class VMPUriProvider : BaseComicUriProvider
    {
        private readonly HtmlWeb _web = new();
        private readonly TimeSpan FILE_TIME_LIMIT = TimeSpan.FromMinutes(1.5);

        public override async Task<int> GetNumberOfItems(Uri uri)
        {
            HtmlDocument doc = await _web.LoadFromWebAsync(uri.AbsoluteUri);
            return GetTheImages(doc).Length;
        }

        public override async Task GetUris(Uri uri, string mainPath, ChannelWriter<DownloadableFile> writer)
        {
            HtmlDocument doc = await _web.LoadFromWebAsync(uri.AbsoluteUri);
            string comicTitle = doc.DocumentNode.SelectSingleNode(@"//div[@class=""comicimg""]//p[1]").InnerText;

            string comicPath = ConstructComicPath(mainPath, comicTitle);
            var imageNodes = GetTheImages(doc);

            //INFO: Getting the uris is faster in this page like in the VCPUriProvider
            await imageNodes.ForParallelAsync(async (int index, HtmlNode imageNode) =>
            {
                Uri imageUri = new(imageNode.Attributes["src"].Value);
                DownloadableFile file = new() { FileName = index, OutputPath = comicPath, FileUri = imageUri, PageUri = uri, TimeLimit = FILE_TIME_LIMIT };
                await writer.WriteAsync(file).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private static HtmlNode[] GetTheImages(HtmlDocument doc)
        {
            var allImageNodes = doc.DocumentNode.SelectNodes(@"//div[@class=""comicimg""]//img");
            var imageNodes = allImageNodes.Where(n => n.Attributes["src"].Value.StartsWith("http")).ToArray();
            return imageNodes;
        }
    }
}