using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Comic_Downloader.CMD.ComicsUriProviders
{
    /// <summary>
    /// Comic implementation of <see cref="IResourceUriProvider"/>.
    /// Contains helper methods that help subclasses implement the methos of <see cref="IResourceUriProvider"/>.
    /// </summary>
    public abstract class BaseResourceUriProvider : IResourceUriProvider
    {
        private static Regex INVALID_CHARS_REGEX;

        protected BaseResourceUriProvider()
        {
            if (INVALID_CHARS_REGEX != null)
                return;
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            INVALID_CHARS_REGEX = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)), RegexOptions.Compiled);
        }

        /// <summary>
        /// Please initialize an instance of this class before calling this method.
        /// Constructs and sanitizes a comic path.
        /// </summary>
        /// <param name="basePath">The path where the comic should be downloaded.</param>
        /// <param name="comicTitle">The title of the comic.</param>
        /// <returns>The sanitized comic path.</returns>
        public static string ConstructComicPath(string basePath, string comicTitle)
        {
            string title = HttpUtility.HtmlDecode(comicTitle.Trim());
            return Path.Combine(basePath, SanitizeFileName(title));
        }

        /// <summary>
        /// Please initialize an instance of this class before calling this method.
        /// Removes all invalid characters from the provided filename.
        /// </summary>
        /// <param name="fileName">The filename that will be sanitized.</param>
        /// <returns>The filename with all the invalid characters removed</returns>
        public static string SanitizeFileName(object fileName) => INVALID_CHARS_REGEX.Replace(fileName.ToString(), string.Empty);

        public abstract Task<int> GetNumberOfItems(Uri uri);

        public abstract IAsyncEnumerable<DownloadableFile> GetUris(Uri uri, string mainPath);
    }
}