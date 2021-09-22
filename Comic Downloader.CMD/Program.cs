using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Comic_Downloader.CMD
{
    internal class Program
    {
        private const int MAX_IMAGES_AT_A_TIME = 15;
        private static HttpClient _httpClient = new HttpClient();

        private static void Main()
        {
            Console.Clear();
            Console.Write("Done Path: ");
            string outputPath = Console.ReadLine().Trim();

            List<Uri> uris = new List<Uri>();
            while (true)
            {
                Console.Write("Link (press only enter to start): ");
                string url = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(url))
                {
                    Console.WriteLine("Starting to download all comics");
                    break;
                }
                else if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                {
                    Console.WriteLine("Please write a valid uri!");
                    continue;
                }
                else
                    uris.Add(uri);
            }

            //List<Uri> uris = new List<Uri>()
            //{
            //    new Uri("https://vercomicsporno.com/cherry-road-7-original-vcp"),
            //    new Uri("https://vercomicsporno.com/hot-sauna-with-hinata-and-nereidas-mom-original-vcp"),
            //    new Uri("https://vercomicsporno.com/incognitymous-sultry-summer-2"),
            //    new Uri("https://vercomicsporno.com/incognitymous-cataratas-lujuriosas-2")
            //};
            //string outputPath = @"D:\elroh\Documents\TestsDownloads";

            IComicsDownloader comicDownloader = new ComicsDownloader(_httpClient, MAX_IMAGES_AT_A_TIME);
            comicDownloader.DownloadReport += (args) => Console.WriteLine($"Progress: {args.CurrentCount}/{args.TotalCount}");
            comicDownloader.DownloadComics(uris.ToArray(), outputPath).Wait();
        }
    }
}