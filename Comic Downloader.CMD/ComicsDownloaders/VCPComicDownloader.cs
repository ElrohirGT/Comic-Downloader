using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD.ComicsUriProviders
{
    /// <summary>
    /// <see cref="IResourceUriProvider"/> implementation for the <see href="vercomicsporno.com"/> host.
    /// </summary>
    public sealed class VCPComicDownloader : BaseResourceUriProvider
    {
        public override async Task<int> GetNumberOfItems(Uri uri)
        {
            var web = new HtmlWeb();
            HtmlDocument document = await web.LoadFromWebAsync(uri.AbsoluteUri).ConfigureAwait(false);

            var imageNodes = document.DocumentNode.SelectNodes(@"//div[@class=""wp-content""]//img");
            return imageNodes.Count;
        }

        public override async Task<IEnumerable<DownloadableFile>> GetUris(Uri uri, string mainPath)
        {
            var web = new HtmlWeb();
            HtmlDocument document = await web.LoadFromWebAsync(uri.AbsoluteUri).ConfigureAwait(false);

            string title = document.DocumentNode.SelectSingleNode(@"//h1[@class=""titl""]").InnerText;
            string comicPath = ConstructComicPath(mainPath, title);

            var imageNodes = document.DocumentNode.SelectNodes(@"//div[@class=""wp-content""]//img");
            DownloadableFile[] files = new DownloadableFile[imageNodes.Count];
            for (int i = 0; i < imageNodes.Count; i++)
                files[i] = new DownloadableFile() { FileName = i, OutputPath = comicPath, Uri = new Uri(imageNodes[i].Attributes["src"].Value) };
            return files;
        }
    }
}