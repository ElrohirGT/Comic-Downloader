﻿using System;

namespace Downloaders.Core
{
    /// <summary>
    /// Represents a file that can be downloaded from the web, and contains all the necessary information to do so.
    /// </summary>
    public struct DownloadableFile
    {
        /// <summary>
        /// The that will be given to the file once it's downloaded.
        /// If it's null, the filename from the uri will be used instead.
        /// </summary>
        public object FileName { get; set; }

        /// <summary>
        /// The path where the file will be downloaded.
        /// It it's null, the user provided output path will be used.
        /// </summary>
        public string? OutputPath { get; set; }

        /// <summary>
        /// The uri of the resource that will be downloaded.
        /// This can't be null.
        /// </summary>
        public Uri Uri { get; set; }
    }
}