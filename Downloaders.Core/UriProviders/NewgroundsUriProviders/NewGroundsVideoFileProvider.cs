using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.NewgroundsUriProviders
{
    internal class NewgroundsResponse
    {
        public string? Author { get; set; }
        public Dictionary<string, NewgroundsSource[]>? Sources { get; set; }
        public string? Title { get; set; }
    }

    internal class NewgroundsSource
    {
        public string? Src { get; set; }
    }

    internal class NewGroundsVideoFileProvider : INewgroundsFileProvider
    {
        private readonly NewgroundsResponse? _response;

        public NewGroundsVideoFileProvider(string htmlResponse)
            => _response = JsonConvert.DeserializeObject<NewgroundsResponse>(htmlResponse);

        public Task<DownloadableFile> GetFile()
        {
            if (_response is null || _response.Sources is null)
                throw new NotSupportedException("The newgrounds response has changed format!");

            DownloadableFile file = new DownloadableFile
            {
                FileName = BaseResourceUriProvider.SanitizeFileName($"[{_response.Author}] {_response.Title}")
            };

            (int Resolution, NewgroundsSource[] Source)? previous = null;
            foreach (KeyValuePair<string, NewgroundsSource[]> item in _response.Sources)
            {
                int resolution = int.Parse(item.Key[0..^1]);
                if (previous is null || previous.Value.Resolution < resolution)
                    previous = (resolution, item.Value);
            }

            file.Uri = new Uri(previous?.Source[0].Src ?? string.Empty);

            return Task.FromResult(file);
        }
    }
}