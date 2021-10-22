using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Downloaders.Core
{
    /// <summary>
    /// Comic implementation of <see cref="IResourceUriProvider"/>.
    /// Contains helper methods that help subclasses implement the methos of <see cref="IResourceUriProvider"/>.
    /// </summary>
    public abstract class BaseResourceUriProvider : IResourceUriProvider
    {
        private static readonly Regex INVALID_CHARS_REGEX
            = new(
                $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]",
                RegexOptions.Compiled
            );

        /// <summary>
        /// Removes all invalid characters from the provided filename.
        /// If a null filename is provided, and empty string is returned.
        /// </summary>
        /// <param name="fileName">The filename that will be sanitized.</param>
        /// <returns>The filename with all the invalid characters removed</returns>
        public static string SanitizeFileName(object fileName)
            => INVALID_CHARS_REGEX.Replace(fileName?.ToString() ?? string.Empty, string.Empty);

        public abstract Task<int> GetNumberOfItems(Uri uri);

        public abstract IAsyncEnumerable<DownloadableFile> GetUris(Uri uri, string mainPath);
    }
}