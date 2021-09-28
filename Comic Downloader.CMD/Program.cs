using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static ConsoleUtilitiesLite.ConsoleUtilities;

namespace Comic_Downloader.CMD
{
    internal class Program
    {
        private const int MAX_IMAGES_AT_A_TIME = 10;
        private static HttpClient _httpClient = new HttpClient();
        private const string LOG_FORMAT = "Progress: {0}/{1}";

        private static readonly string[] _title = new string[]
        {
            "█▀▄ █▀█ █░█░█ █▄░█ █░░ █▀█ ▄▀█ █▀▄ █▀▀ █▀█",
            "█▄▀ █▄█ ▀▄▀▄▀ █░▀█ █▄▄ █▄█ █▀█ █▄▀ ██▄ █▀▄"
        };

        private static void Main()
        {
            Console.Clear();
            ShowTitle(_title);
            ShowVersion(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

            Console.Write("Done Path: ");
            string outputPath = Console.ReadLine().Trim();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                outputPath = Regex.Unescape(outputPath);
            outputPath = Path.GetFullPath(outputPath);
            LogWarningMessage($"Path that will be used: {outputPath}");

            List<Uri> uris = new List<Uri>();
            while (true)
            {
                Console.Write("Link (press only enter to start): ");
                string url = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(url))
                    break;
                else if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                    LogErrorMessage("Please write a valid uri!");
                else if (uris.Contains(uri))
                    LogErrorMessage("Please don't repeat uris");
                else
                    uris.Add(uri);
            }

            //List<Uri> uris = new List<Uri>()
            //{
            //    new Uri("https://vermangasporno.com/doujin/33515.html"),
            //    new Uri("https://vercomicsporno.com/cherry-road-7-original-vcp"),
            //    new Uri("https://vercomicsporno.com/hot-sauna-with-hinata-and-nereidas-mom-original-vcp"),
            //    new Uri("https://vercomicsporno.com/incognitymous-sultry-summer-2"),
            //    new Uri("https://vercomicsporno.com/incognitymous-cataratas-lujuriosas-2"),
            //    //new Uri("https://e-hentai.org/g/2017110/463359c6ce/"),
            //    new Uri("https://e-hentai.org/g/2017266/d916aea2de/"),
            //    new Uri("https://e-hentai.org/g/2017115/cb506df526/"),
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