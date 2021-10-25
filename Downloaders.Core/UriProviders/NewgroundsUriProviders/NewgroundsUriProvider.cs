using CefSharp;
using CefSharp.OffScreen;
using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.NewgroundsUriProviders
{
    internal sealed class NewgroundsUriProvider : BaseResourceUriProvider
    {
        public NewgroundsUriProvider()
        {
            var settings = new CefSettings
            {
                LogSeverity = LogSeverity.Disable,
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            };
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
        }

        public override Task<int> GetNumberOfItems(Uri uri) => Task.FromResult(1);

        public override async Task GetUris(Uri uri, string mainPath, ChannelWriter<DownloadableFile> writer)
        {
            using var browser = new ChromiumWebBrowser(uri.AbsoluteUri);
            INewgroundsFileProvider provider = await NewgroundsFileUriProviderFactory.GetProvider(browser).ConfigureAwait(false);
            DownloadableFile file = await provider.GetFile().ConfigureAwait(false);
            await writer.WriteAsync(file).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                Cef.Shutdown();
        }
    }
}