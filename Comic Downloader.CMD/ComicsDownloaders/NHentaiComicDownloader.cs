using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    public sealed class NHentaiComicDownloader : BaseComicDownloader
    {
        private const int MIN_WAIT_MS = 350;
        private const int MAX_WAIT_MS = 700;
        private const int MAX_SIMULTANEOUS_REQUESTS = 3;
        private HtmlWeb _web = new();

        //nhentai has a hard limit to how many request can be done at the same time,
        //this gate and the random number generator aim to limit the amount of requests to the page.
        private SemaphoreSlim _gate = new(MAX_SIMULTANEOUS_REQUESTS);

        private Random _generator = new();

        protected override async Task Download_Comic(Uri uri, string basePath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors)
        {
            HtmlDocument document = await MakeCallToWebSiteAsync(() => _web.LoadFromWebAsync(uri.AbsoluteUri)).ConfigureAwait(false);
            int numberOfImages = await Get_Number_Of_Images(uri).ConfigureAwait(false);

            StringBuilder comicTitleBuilder = new();
            HtmlNodeCollection titleNodes = document.DocumentNode.SelectNodes(@"//h1[@class=""title""]//span");
            foreach (var titleNode in titleNodes)
                comicTitleBuilder.Append(titleNode.InnerText.Trim() + " ");
            string comicPath = ConstructComicPath(basePath, comicTitleBuilder.ToString());

            List<Task> tasks = new List<Task>(numberOfImages);
            for (int i = 1; i <= numberOfImages; i++)
            {
                string imageURL = uri.AbsoluteUri + i;
                HtmlDocument imageDoc = await MakeCallToWebSiteAsync(() => _web.LoadFromWebAsync(imageURL)).ConfigureAwait(false);
                HtmlNode imgNode = imageDoc.DocumentNode.SelectSingleNode(@"//div[@id=""content""]//img");

                Task t = MakeCallToWebSiteAsync(() => DownloadFileAsync(comicPath, new Uri(imgNode.Attributes["src"].Value), gate, httpClient, errors, i - 1));
                tasks.Add(t);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        protected override async Task<int> Get_Number_Of_Images(Uri uri)
        {
            HtmlDocument document = await MakeCallToWebSiteAsync(() => _web.LoadFromWebAsync(uri.AbsoluteUri)).ConfigureAwait(false);
            HtmlNode node = document.DocumentNode.SelectSingleNode(@"//a[@class=""tag""][last()]/span");
            int numberOfImages = int.Parse(node.InnerText);
            return numberOfImages;
        }

        private async Task MakeCallToWebSiteAsync(Func<Task> func)
        {
            try
            {
                await _gate.WaitAsync().ConfigureAwait(false);
                await func.Invoke().ConfigureAwait(false);
                await Task.Delay(_generator.Next(MIN_WAIT_MS, MAX_WAIT_MS)).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<T> MakeCallToWebSiteAsync<T>(Func<Task<T>> func)
        {
            try
            {
                await _gate.WaitAsync().ConfigureAwait(false);
                T result = await func.Invoke().ConfigureAwait(false);
                await Task.Delay(_generator.Next(MIN_WAIT_MS, MAX_WAIT_MS)).ConfigureAwait(false);
                return result;
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}