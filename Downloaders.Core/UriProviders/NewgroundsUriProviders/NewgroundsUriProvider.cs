using CefSharp.OffScreen;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.NewgroundsUriProviders
{
    internal sealed class NewgroundsUriProvider : BaseResourceUriProvider
    {
        private const int MAX_BROWSERS_CONCURRENTLY = 1;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(MAX_BROWSERS_CONCURRENTLY);

        public override Task<int> GetNumberOfItems(Uri uri) => Task.FromResult(1);

        public override async Task GetUris(Uri uri, string mainPath, ChannelWriter<DownloadableFile> writer)
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);

                using var browser = new ChromiumWebBrowser(uri.AbsoluteUri);
                INewgroundsFileProvider provider = await NewgroundsFileUriProviderFactory.GetProvider(browser).ConfigureAwait(false);

                DownloadableFile file = await provider.GetFile().ConfigureAwait(false);
                await writer.WriteAsync(file).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}