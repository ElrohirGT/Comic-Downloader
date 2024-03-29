﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Downloaders.Core;

using static ConsoleUtilitiesLite.ConsoleUtilities;

namespace Comic_Downloader.CMD;

internal class Program
{
    private const string LOG_FORMAT = "Progress: {0}/{1}";
    private const int MAX_IMAGES_AT_A_TIME = 15;

    private static readonly HttpClient _httpClient = new();

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

#if !DEBUG
            Console.Write("Output Path: ");
            string lineRead = Console.ReadLine().Trim();
            string outputPath = string.IsNullOrEmpty(lineRead) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : lineRead;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                outputPath = Regex.Unescape(outputPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                outputPath = outputPath.Replace("\"", string.Empty).Trim();
            outputPath = Path.GetFullPath(outputPath);
            LogWarningMessage($"Path that will be used: {outputPath}");
            SubDivision();
            LogInfoMessage("Press enter twice to start downloading. Enter d to delete previous link.");

            Stack<Uri> uris = new();
            bool previousWasError = false;
            while (true)
            {
                Console.Write("Link: ");
                string url = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(url))
                    break;
                else if (url == "d" && uris.TryPop(out _))
                {
                    int lines = previousWasError ? 3 : 2;
                    ClearPreviousLog(lines * Console.BufferWidth);
                    previousWasError = false;
                }
                else if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                {
                    int lines = previousWasError ? 2 : 1;
                    ClearPreviousLog(lines * Console.BufferWidth);
                    LogErrorMessage("Please write a valid uri!");
                    previousWasError = true;
                }
                else if (uris.Contains(uri))
                {
                    int lines = previousWasError ? 2 : 1;
                    ClearPreviousLog(lines * Console.BufferWidth);
                    LogErrorMessage("Please don't repeat uris");
                    previousWasError = true;
                }
                else
                {
                    uris.Push(uri);
                    if (previousWasError)
                    {
                        ClearPreviousLog(2 * Console.BufferWidth);
                        Console.WriteLine($"Link: {uri}");
                    }
                    previousWasError = false;
                }
            }
#endif
#if DEBUG
        List<Uri> uris = new List<Uri>()
            {
                new Uri("https://e-hentai.org/g/2017266/d916aea2de/"),
            new Uri("https://vermangasporno.com/doujin/33515.html"),
            new Uri("https://vercomicsporno.com/cherry-road-7-original-vcp"),
            //new Uri("https://e-hentai.org/g/2017115/cb506df526/"),
            new Uri("https://vercomicsporno.com/hot-sauna-with-hinata-and-nereidas-mom-original-vcp"),
            new Uri("https://vercomicsporno.com/incognitymous-sultry-summer-2"),
            //new Uri("https://e-hentai.org/g/2039222/4086a69148/"),
            new Uri("https://vercomicsporno.com/incognitymous-cataratas-lujuriosas-2"),
            //new Uri("https://e-hentai.org/g/1809818/04dc69cf64/"),
            //    new Uri("https://www.newgrounds.com/portal/view/765377"),
            //    new Uri("https://www.newgrounds.com/portal/view/819297"),
            //    new Uri("https://www.newgrounds.com/art/view/diives/heketa-s-husband-treat-teaser"),
            //    new Uri("https://www.newgrounds.com/art/view/lewdua/alice-2")
            };
        string outputPath = @"D:\elroh\Documents\TestsDownloads2";
#endif

        using IDownloader comicDownloader = new Downloader(_httpClient, MAX_IMAGES_AT_A_TIME);
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
        IDictionary<Uri, ICollection<string>> errors = comicDownloader.DownloadFiles(uris, outputPath).Result;
        DateTime after = DateTime.Now;

        SubDivision();
        LogInfoMessage($"Time: {after - before} -- Success Rate: {(1 - errors.Values.Sum(l => l.Count) / Math.Max(maxCount, 1f)) * 100}%");
        Console.ReadLine();

        if (errors.Count == 0)
            LogSuccessMessage("NO ERRORS");
        else
        {
            LogErrorMessage("ERRORS:");
            foreach (var uriErrors in errors.Values)
                foreach (var error in uriErrors)
                    LogErrorMessage(error);

            LogInfoMessage("Writing all uris with an error in the log file...");
            File.WriteAllLines(Path.Combine(outputPath, "uris.txt"), errors.Keys.Select(uri => uri.ToString()));
            LogInfoMessage("Finished!");
            Console.ReadLine();
        }
    }
}
