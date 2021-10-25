using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Downloaders.Core.UriProviders.NewgroundsUriProviders
{
    internal class NewgroundsFileUriProviderFactory
    {
        private const string INTERCEPT_AJAX_CALL_SCRIPT = "(async function(){await CefSharp.BindObjectAsync('videoResponseReceiver');var proxied=window.XMLHttpRequest.prototype.send;window.XMLHttpRequest.prototype.send=function(){console.log(arguments);var pointer=this;var intervalId=window.setInterval(function(){if(pointer.readyState!=4){return}videoResponseReceiver.sendResponse(pointer.responseText);clearInterval(intervalId)},1);return proxied.apply(this,[].slice.call(arguments))}})();";
        private const string IS_IMAGE_SCRIPT = "document.querySelectorAll('div.image').length == 1";
        private const string IS_VIDEO_SCRIPT = "document.querySelectorAll('div.play-wrapper').length == 1";

        public static Task<INewgroundsFileProvider> GetProvider(ChromiumWebBrowser browser)
        {
            TaskCompletionSource<INewgroundsFileProvider> tcs = new();
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(20));//INFO: Cancel the operation if 20 seconds have passed.

            browser.LoadingStateChanged += PageLoaded;
            async void PageLoaded(object? sender, LoadingStateChangedEventArgs e)
            {
                if (e.IsLoading)
                    return;

                var isVideoResponse = await browser.EvaluateScriptAsync(IS_VIDEO_SCRIPT);
                var isImageResponse = await browser.EvaluateScriptAsync(IS_IMAGE_SCRIPT);

                bool isVideo = bool.Parse(isVideoResponse.Result?.ToString() ?? bool.FalseString);
                bool isImage = bool.Parse(isImageResponse.Result?.ToString() ?? bool.FalseString);

                if (isVideo)
                {
                    //INFO: This calls the ResponseReceived method below
                    browser.JavascriptObjectRepository.Register("videoResponseReceiver", new VideoResponseReceiver { ResponseAction = ResponseReceived });
                    await browser.EvaluateScriptAsync(INTERCEPT_AJAX_CALL_SCRIPT);

                    string movieId = browser.Address.Split('/')[^1];
                    string script = $@"
                        ngutils.components.video.global_player.initialized = true;
                        ngutils.components.video.global_player.loadMovieByID({movieId})
                    ";
                    await browser.EvaluateScriptAsync(script);
                }
                else if (isImage)
                {
                    string pageHtml = await browser.GetBrowser().MainFrame.GetSourceAsync();
                    tcs.SetResult(new NewGroundsImageFileProvider(pageHtml));
                }
            }
            void ResponseReceived(string html)
            {
                if (string.IsNullOrEmpty(html))
                    tcs.SetException(new ArgumentNullException("The provided response is null or empty"));
                tcs.SetResult(new NewGroundsVideoFileProvider(html));
            }

            while (!tcs.Task.IsCompleted)
                cts.Token.ThrowIfCancellationRequested();
            return tcs.Task;
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