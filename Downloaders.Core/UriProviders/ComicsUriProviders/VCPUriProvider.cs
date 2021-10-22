using HtmlAgilityPack;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.ComicsUriProviders
{
    /// <summary>
    /// <see cref="IResourceUriProvider"/> implementation for the <see href="vercomicsporno.com"/> host.
    /// </summary>
    public sealed class VCPUriProvider : BaseComicUriProvider
    {
        private HtmlWeb _web = new();

        public override async Task<int> GetNumberOfItems(Uri uri)
        {
            HtmlDocument document = await _web.LoadFromWebAsync(uri.AbsoluteUri).ConfigureAwait(false);
            HtmlNodeCollection imageNodes = GetImages(document);
            return imageNodes.Count;
        }

        public override async Task GetUris(Uri uri, string mainPath, ChannelWriter<DownloadableFile> writer)
        {
            HtmlDocument document = await _web.LoadFromWebAsync(uri.AbsoluteUri).ConfigureAwait(false);
            string title = document.DocumentNode.SelectSingleNode(@"//h1[@class=""titl""]").InnerText;

            string comicPath = ConstructComicPath(mainPath, title);
            var imageNodes = GetImages(document);

            //INFO: Getting uris from this web page is faster so the batch will contain every image of the comic
            await imageNodes.ForParallelAsync(async (int index, HtmlNode imageNode) =>
            {
                Uri imageUri = new(imageNode.Attributes["src"].Value);
                DownloadableFile file = new() { FileName = index, OutputPath = comicPath, Uri = imageUri };
                await writer.WriteAsync(file).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private static HtmlNodeCollection GetImages(HtmlDocument document)
            => document.DocumentNode.SelectNodes(@"//div[@class=""wp-content""]//img");
    }
}