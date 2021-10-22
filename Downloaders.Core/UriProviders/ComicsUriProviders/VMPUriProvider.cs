﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.ComicsUriProviders
{
    /// <summary>
    /// <see cref="IResourceUriProvider"/> implementation for the <see href="vermangasporno.com"/> host.
    /// </summary>
    public sealed class VMPUriProvider : BaseComicUriProvider
    {
        private readonly HtmlWeb _web = new();

        public override async Task<int> GetNumberOfItems(Uri uri)
        {
            HtmlDocument doc = await _web.LoadFromWebAsync(uri.AbsoluteUri);
            return GetTheImages(doc).Length;
        }

        public override async IAsyncEnumerable<DownloadableFile> GetUris(Uri uri, string mainPath)
        {
            HtmlDocument doc = await _web.LoadFromWebAsync(uri.AbsoluteUri);
            string comicTitle = doc.DocumentNode.SelectSingleNode(@"//div[@class=""comicimg""]//p[1]").InnerText;

            string comicPath = ConstructComicPath(mainPath, comicTitle);
            var imageNodes = GetTheImages(doc);

            //INFO: Getting the uris is faster in this page like in the VCPUriProvider
            DownloadableFile[] batch = new DownloadableFile[imageNodes.Length];
            Parallel.For(0, imageNodes.Length, (int i) =>
            {
                Uri imageUri = new(imageNodes[i].Attributes["src"].Value);
                DownloadableFile file = new() { FileName = i, OutputPath = comicPath, Uri = imageUri };
                batch[i] = file;
            });

            foreach (var file in batch)
                yield return file;
        }

        private static HtmlNode[] GetTheImages(HtmlDocument doc)
        {
            var allImageNodes = doc.DocumentNode.SelectNodes(@"//div[@class=""comicimg""]//img");
            var imageNodes = allImageNodes.Where(n => n.Attributes["src"].Value.StartsWith("http")).ToArray();
            return imageNodes;
        }
    }
}