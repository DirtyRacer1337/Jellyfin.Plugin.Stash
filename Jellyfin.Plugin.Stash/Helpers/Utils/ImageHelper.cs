using System;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace Stash.Helpers.Utils
{
    internal static class ImageHelper
    {
        public static async Task<string> GetImageSizeAndValidate(string url, CancellationToken cancellationToken)
        {
            if (url != null)
            {
                var http = await HTTP.Request(url, cancellationToken).ConfigureAwait(false);
                if (http.IsOK)
                {
                    SKImage img = null;

                    try
                    {
                        img = SKImage.FromEncodedData(http.ContentStream);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"ImageHelper error: \"{e}\"");
                    }

                    if (img != null && img.Width > 100)
                    {
                        return url;
                    }
                }
            }

            return null;
        }
    }
}