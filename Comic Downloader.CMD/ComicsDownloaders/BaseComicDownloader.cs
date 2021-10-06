using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Comic_Downloader.CMD.ComicsDownloaders
{
    /// <summary>
    /// Comic implementation of <see cref="IResourceDownloader"/>.
    /// Contains helper methods that help subclasses implement the methos of <see cref="IResourceDownloader"/>.
    /// </summary>
    public abstract class BaseComicDownloader : BaseResourceDownloader
    {
        protected BaseComicDownloader()
            : base("Error downloading comic: {0}\n{1}", "Error getting the number of images of: {0}\n{1}")
        {
        }

        protected BaseComicDownloader(string errorDownloadingFormat, string errorGettingNumberOfItemsFormat)
            : base(errorDownloadingFormat, errorGettingNumberOfItemsFormat)
        {
        }

        /// <summary>
        /// Do not override this method in child classes.
        /// Override <see cref="Get_Number_Of_Images(Uri)"/> instead.
        /// </summary>
        protected override Task<int> Get_Number_Of_Items(Uri uri) => Get_Number_Of_Images(uri);

        /// <summary>
        /// Do not override this method in child classes.
        /// Override <see cref="Download_Comic(Uri, string, HttpClient, SemaphoreSlim, BlockingCollection{string})"/> instead.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="mainPath"></param>
        /// <param name="httpClient"></param>
        /// <param name="gate"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        protected override Task _Download(Uri uri, string mainPath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors)
            => Download_Comic(uri, mainPath, httpClient, gate, errors);

        /// <summary>
        /// Override this method to implement the action of downloading the comic.
        /// The <paramref name="errors"/> collection is passed, and is intended to be used only to pass a reference to the method
        /// <see cref="BaseResourceDownloader.DownloadFileAsync(string, Uri, SemaphoreSlim, HttpClient, BlockingCollection{string}, object)"/>.
        /// Any exception that is thrown in this method will be correctly handled by <see cref="BaseComicDownloader"/>,
        /// so you don't need to try/catch this method.
        /// A sub-folder of <paramref name="basePath"/> should be created with the name of the comic, and the images should be downloaded there.
        /// </summary>
        /// <param name="uri">The uri where the comic is located.</param>
        /// <param name="basePath">The path where the comic will be downloaded.</param>
        /// <param name="httpClient">The client to use.</param>
        /// <param name="gate">This allows to have a control on how many images are downloaded at a time.</param>
        /// <param name="errors">The errors collection</param>
        /// <returns>
        /// A task that completes once the comic has been downloaded or an error ocurred downloading the comic.
        /// It doesn't stops for image errors.
        /// </returns>
        protected abstract Task Download_Comic(Uri uri, string basePath, HttpClient httpClient, SemaphoreSlim gate, BlockingCollection<string> errors);

        /// <summary>
        /// Override this method to implement the action of getting the number of images.
        /// You don't need to try/catch this method, <see cref="BaseComicDownloader"/> already handles any exception this method could throw.
        /// </summary>
        /// <param name="uri">The uri where the comic resides.</param>
        /// <returns>The number of images the comic has.</returns>
        protected abstract Task<int> Get_Number_Of_Images(Uri uri);

        /// <summary>
        /// Constructs and sanitizes a comic path.
        /// </summary>
        /// <param name="basePath">The path where the comic should be downloaded.</param>
        /// <param name="comicTitle">The title of the comic.</param>
        /// <returns>The sanitized comic path.</returns>
        protected string ConstructComicPath(string basePath, string comicTitle)
        {
            string title = HttpUtility.HtmlDecode(comicTitle.Trim());
            return Path.Combine(basePath, SanitizeFileName(title));
        }
    }
}