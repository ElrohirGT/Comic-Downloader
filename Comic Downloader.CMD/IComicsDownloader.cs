using System;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    internal interface IComicsDownloader
    {
        event Action<DownloadReportEventArgs> DownloadReport;

        Task DownloadComic(Uri url, string mainPath);

        Task DownloadComics(Uri[] urls, string outputPath);
    }
}