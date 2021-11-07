using PuppeteerSharp;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.NewgroundsUriProviders
{
    internal class NewgroundsFileUriProviderFactory
    {
        private const string GET_VIDEO_API_RESPONSE = "return new Promise((resolve,reject)=>{(async function(){var proxied=window.XMLHttpRequest.prototype.send;window.XMLHttpRequest.prototype.send=function(){console.log(arguments);var pointer=this;var intervalId=window.setInterval(function(){if(pointer.readyState!=4){return}resolve(pointer.responseText)clearInterval(intervalId)},1);return proxied.apply(this,[].slice.call(arguments))}})();ngutils.components.video.global_player.initialized=true;ngutils.components.video.global_player.loadMovieByID({0})});";
        private const string IS_IMAGE_SCRIPT = "document.querySelectorAll('div.image').length == 1";

        private const string IS_VIDEO_SCRIPT = "document.querySelectorAll('div.play-wrapper').length == 1";
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(20);

        public static async Task<INewgroundsFileProvider> GetProvider(Page page)

        {
            //INFO: Cancel the operation if 20 seconds have passed.
            using CancellationTokenSource cts = new(TIMEOUT);
            TaskCompletionSource<INewgroundsFileProvider> tcs = new();

            bool isVideo = await page.EvaluateExpressionAsync<bool>(IS_VIDEO_SCRIPT);
            bool isImage = await page.EvaluateExpressionAsync<bool>(IS_IMAGE_SCRIPT);

            if (isImage)
            {
                string pageHtml = await page.GetContentAsync();
                tcs.SetResult(new NewGroundsImageFileProvider(pageHtml));
            }
            else if (isVideo)
            {
                string movieId = page.Url.Split('/')[^1];
                string script = $"new Promise((e,o)=>{{!async function(){{var o=window.XMLHttpRequest.prototype.send;window.XMLHttpRequest.prototype.send=function(){{console.log(arguments);var t=this,n=window.setInterval(function(){{4==t.readyState&&(e(t.responseText),clearInterval(n))}},1);return o.apply(this,[].slice.call(arguments))}}}}(),ngutils.components.video.global_player.initialized=!0,ngutils.components.video.global_player.loadMovieByID({movieId})}});";
                string apiResponse = await page.EvaluateExpressionAsync<string>(script);
                tcs.SetResult(new NewGroundsVideoFileProvider(apiResponse));
            }

            while (!tcs.Task.IsCompleted)
                if (cts.Token.IsCancellationRequested)
                    throw new TimeoutException($"TIMEOUT: The url for the video could not be found within: {TIMEOUT}");
            return tcs.Task.Result;
        }

        /// <summary>
        /// Used for binding the js code with the c# code
        /// </summary>
        private class VideoResponseReceiver
        {
            public Action<string>? ResponseAction { get; set; }

            /// <summary>
            /// This functions is called from the js code,
            /// in the js code camel case is used, so this will be called with sendRedponse.
            /// </summary>
            /// <param name="response">The response text of the request</param>
            public void SendResponse(string response)
            {
                ResponseAction?.Invoke(response);
            }
        }
    }
}