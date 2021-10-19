using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsUriProviders
{
    /// <summary>
    /// <see cref="IResourceUriProvider"/> implementation for the <see href="vercomicsporno.com"/> host.
    /// </summary>
    public sealed class VCPUriProvider : BaseResourceUriProvider
    {
        public override async Task<int> GetNumberOfItems(Uri uri)
        {
            var web = new HtmlWeb();
            HtmlDocument document = await web.LoadFromWebAsync(uri.AbsoluteUri).ConfigureAwait(false);
            HtmlNodeCollection imageNodes = GetImages(document);
            return imageNodes.Count;
        }

        public override async IAsyncEnumerable<DownloadableFile> GetUris(Uri uri, string mainPath)
        {
            var web = new HtmlWeb();
            HtmlDocument document = await web.LoadFromWebAsync(uri.AbsoluteUri).ConfigureAwait(false);

            string title = document.DocumentNode.SelectSingleNode(@"//h1[@class=""titl""]").InnerText;
            string comicPath = ConstructComicPath(mainPath, title);

            var imageNodes = GetImages(document);
            //INFO: Getting uris from this web page is faster so the batch will contain every image of the comic
            DownloadableFile[] batch = new DownloadableFile[imageNodes.Count];

            for (int i = 0; i < imageNodes.Count; i++)
            {
                Uri imageUri = new Uri(imageNodes[i].Attributes["src"].Value);
                DownloadableFile file = new DownloadableFile() { FileName = i, OutputPath = comicPath, Uri = imageUri };
                batch[i] = file;
            }

            foreach (var file in batch)
                yield return file;
        }

        private static HtmlNodeCollection GetImages(HtmlDocument document)
            => document.DocumentNode.SelectNodes(@"//div[@class=""wp-content""]//img");
    }
}