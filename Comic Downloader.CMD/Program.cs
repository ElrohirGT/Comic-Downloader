using System;
using System.Collections.Generic;
using System.Net.Http;
using static ConsoleUtilitiesLite.ConsoleUtilities;

namespace Comic_Downloader.CMD
{
    internal class Program
    {
        private const int MAX_IMAGES_AT_A_TIME = 10;
        private static HttpClient _httpClient = new HttpClient();
        private const string LOG_FORMAT = "Progress: {0}/{1}";

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
                    Console.WriteLine("Please write a valid uri!");
                else if (uris.Contains(uri))
                    Console.WriteLine("Please don't repeat uris");
                else
                    uris.Add(uri);
            }

            //List<Uri> uris = new List<Uri>()
            //{
            //    new Uri("https://vercomicsporno.com/cherry-road-7-original-vcp"),
            //    new Uri("https://vercomicsporno.com/hot-sauna-with-hinata-and-nereidas-mom-original-vcp"),
            //    new Uri("https://vercomicsporno.com/incognitymous-sultry-summer-2"),
            //    new Uri("https://vercomicsporno.com/incognitymous-cataratas-lujuriosas-2"),
            //    //new Uri("https://e-hentai.org/g/2017110/463359c6ce/"),
            //    new Uri("https://e-hentai.org/g/2017266/d916aea2de/"),
            //    new Uri("https://e-hentai.org/g/2017115/cb506df526/"),
            //};

            //List<Uri> uris = new List<Uri>()
            //{
            //    new Uri("https://e-hentai.org/g/1552322/4aa3db0431/"),
            //    new Uri("https://e-hentai.org/g/1650036/3abab70521/"),
            //    new Uri("https://e-hentai.org/g/1989691/624c67a6c3/"),
            //    new Uri("https://e-hentai.org/g/607563/308ba2014d/"),
            //    new Uri("https://e-hentai.org/g/913930/6043ef52e0/"),
            //    //new Uri("https://e-hentai.org/g/913931/105605c620/"), // this one doesn't load the comic preview page at the start.
            //    new Uri("https://e-hentai.org/g/939836/8b7fc17ddc/"),
            //    new Uri("https://e-hentai.org/g/1062251/82136d6f5f/"),
            //    new Uri("https://e-hentai.org/g/1087916/0454d7706e/"),
            //    new Uri("https://e-hentai.org/g/1132372/46b48e076b/"),
            //    new Uri("https://e-hentai.org/g/1184993/91095bf505/"),
            //    new Uri("https://e-hentai.org/g/1405667/c80e2f1c6f/"),
            //    new Uri("https://e-hentai.org/g/1436452/5055b3a906/"),
            //    new Uri("https://e-hentai.org/g/1522122/be1baca4bd/")
            //};
            //string outputPath = @"D:\elroh\Documents\TestsDownloads";

            IComicsDownloader comicDownloader = new ComicsDownloader(_httpClient, MAX_IMAGES_AT_A_TIME);
            SubDivision();
            LogSuccessMessage("Starting Downloads...");

            int previousLogLength = LogInfoMessage(string.Format(LOG_FORMAT, 0, 0));
            int maxCount = 1;
            comicDownloader.DownloadReport += (args) =>
            {
                ClearPreviousLog(previousLogLength);
                previousLogLength = LogInfoMessage(LOG_FORMAT, args.CurrentCount, args.TotalCount);
                maxCount = args.TotalCount;
            };

            DateTime before = DateTime.Now;
            string[] errors = comicDownloader.DownloadComics(uris.ToArray(), outputPath).Result;
            DateTime after = DateTime.Now;

            SubDivision();
            LogInfoMessage($"Time: {after - before} -- Success Rate: {(1 - errors.Length / Math.Max(maxCount, 1f)) * 100}%");
            Console.ReadLine();

            if (errors.Length == 0)
                LogSuccessMessage("NO ERRORS");
            else
            {
                LogErrorMessage("ERRORS:");
                foreach (var error in errors)
                    LogErrorMessage(error);
                Console.ReadLine();
            }
        }
    }
}