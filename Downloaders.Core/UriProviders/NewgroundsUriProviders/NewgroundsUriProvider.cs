using PuppeteerSharp;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.NewgroundsUriProviders
{
    internal sealed class NewgroundsUriProvider : BaseResourceUriProvider
    {
        //INFO: The browser throws an exception eventually if more than 1 browser is used.
        private const int MAX_BROWSERS_CONCURRENTLY = 1;

        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(MAX_BROWSERS_CONCURRENTLY);

        public override Task<int> GetNumberOfItems(Uri uri) => Task.FromResult(1);

        public override async Task GetUris(Uri uri, string mainPath, ChannelWriter<DownloadableFile> writer)
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);

                await new BrowserFetcher().DownloadAsync().ConfigureAwait(false);
                LaunchOptions options = new() { Headless = true };
                using var browser = await Puppeteer.LaunchAsync(options).ConfigureAwait(false);

                var page = await browser.NewPageAsync();
                await page.GoToAsync(uri.AbsoluteUri, WaitUntilNavigation.DOMContentLoaded);
                INewgroundsFileProvider provider = await NewgroundsFileUriProviderFactory.GetProvider(page).ConfigureAwait(false);

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