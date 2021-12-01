using HtmlAgilityPack;
using System;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.NewgroundsUriProviders
{
    internal struct NewGroundsImageFileProvider : INewgroundsFileProvider
    {
        private HtmlDocument _doc;
        readonly TimeSpan FILE_TIME_LIMIT = TimeSpan.FromMinutes(1.5);

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

            //INFO: The page uri is set by the NewgroundsUriProvider
            DownloadableFile downloadableFile = new()
            {
                FileUri = uri,
                FileName = title,
                TimeLimit = FILE_TIME_LIMIT,
            };
            return Task.FromResult(downloadableFile);
        }
    }
}