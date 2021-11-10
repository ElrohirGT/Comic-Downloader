using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

using Moq;

using Xunit;

namespace Downloaders.Core.Tests;
public class DownloaderTests
{
    readonly Uri TEST_URI = new("https://www.google.com/");
    
    [Fact]
    public async Task GetsNumberOfPages()
    {
        Mock<IResourceUriProvider> uriProviderMock = new();
        uriProviderMock.Setup(p => p.GetNumberOfItems(TEST_URI).Result).Returns(0);
        var dic = new Dictionary<string, IResourceUriProvider>()
        {
            { "www.google.com", uriProviderMock.Object }
        };
        Downloader downloader = new(new System.Net.Http.HttpClient(), dic);
        
        await downloader.DownloadFiles(new Uri[] { TEST_URI }, "C:");

        uriProviderMock.Verify(m=>m.GetNumberOfItems(TEST_URI), Times.Once);
    }

    [Fact]
    public async Task GetsUris()
    {
        Mock<IResourceUriProvider> uriProviderMock = new();
        var channel = Channel.CreateUnbounded<DownloadableFile>();
        uriProviderMock.Setup(p => p.GetUris(TEST_URI, "C:", channel.Writer));
        var dic = new Dictionary<string, IResourceUriProvider>()
        {
            {"www.google.com", uriProviderMock.Object }
        };
        Downloader downloader = new(new System.Net.Http.HttpClient(), dic);

        await downloader.DownloadFiles(new Uri[] { TEST_URI }, "C:", channel);

        uriProviderMock.Verify(m => m.GetUris(TEST_URI, "C:", channel.Writer), Times.Once);
    }
}