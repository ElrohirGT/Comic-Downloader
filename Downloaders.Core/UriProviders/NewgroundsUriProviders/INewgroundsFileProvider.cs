using System;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.NewgroundsUriProviders
{
    internal interface INewgroundsFileProvider
    {
        Task<DownloadableFile> GetFile();
    }
}