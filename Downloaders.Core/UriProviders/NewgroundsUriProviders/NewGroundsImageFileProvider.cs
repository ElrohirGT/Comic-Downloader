using HtmlAgilityPack;
using System;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.NewgroundsUriProviders
{
    internal struct NewGroundsImageFileProvider : INewgroundsFileProvider
    {
        private HtmlDocument _doc;

        public NewGroundsImageFileProvider(string html)
        {
            _doc = new HtmlDocument();
            _doc.LoadHtml(html);
        }

        public Task<DownloadableFile> GetFile()
        {
            var node = _doc.DocumentNode.SelectSingleNode(@"//div[@class=""image""]//img");
            var uri = new Uri(node.Attributes["src"].Value);

            node = _doc.DocumentNode.SelectSingleNode(@"//div[@class=""pod-head""]//h2");
            var title = BaseResourceUriProvider.SanitizeFileName(node.InnerText);

            DownloadableFile downloadableFile = new DownloadableFile
            {
                Uri = uri,
                FileName = title
            };
            return Task.FromResult(downloadableFile);
        }
    }
}