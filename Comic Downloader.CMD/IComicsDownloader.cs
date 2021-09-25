using System;
using System.Threading.Tasks;

namespace Comic_Downloader.CMD
{
    public interface IComicsDownloader
    {
        event Action<DownloadReportEventArgs> DownloadReport;

        Task<string[]> DownloadComic(Uri url, string mainPath);

        Task<string[]> DownloadComics(Uri[] urls, string outputPath);
    }
}