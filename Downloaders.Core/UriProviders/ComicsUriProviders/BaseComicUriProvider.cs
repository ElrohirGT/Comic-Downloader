using System.IO;
using System.Web;

namespace Downloaders.Core.UriProviders.ComicsUriProviders
{
    /// <summary>
    /// Represents the base class for all comic uri providers.
    /// Contains some handy methods that help in the construction of <see cref="DownloadableFile"/>'s for comics.
    /// </summary>
    public abstract class BaseComicUriProvider : BaseResourceUriProvider
    {
        /// <summary>
        /// Constructs and sanitizes a comic path.
        /// </summary>
        /// <param name="basePath">The path where the comic should be downloaded.</param>
        /// <param name="comicTitle">The title of the comic.</param>
        /// <returns>The sanitized comic path.</returns>
        protected static string ConstructComicPath(string basePath, string comicTitle)
        {
            string title = HttpUtility.HtmlDecode(comicTitle.Trim());
            return Path.Combine(basePath, SanitizeFileName(title));
        }
    }
}