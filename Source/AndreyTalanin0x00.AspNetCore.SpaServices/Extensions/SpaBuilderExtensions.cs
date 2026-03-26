using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices;

namespace AndreyTalanin0x00.AspNetCore.SpaServices.Extensions;

/// <summary>
/// Provides a set of extension methods for the <see cref="ISpaBuilder" /> <see langword="interface" />.
/// </summary>
public static class SpaBuilderExtensions
{
    /// <summary>
    /// Disables response compression so it is possible to inject a browser refresh script.
    /// </summary>
    /// <param name="spaBuilder">The Single Page Application (SPA) builder.</param>
    /// <remarks>
    /// Fixes the following warning:
    /// "Unable to configure browser refresh script injection on the response.
    /// This may have been caused by the response's Content-Encoding: 'gzip'.
    /// Consider disabling response compression."
    /// </remarks>
    public static void UseEmptyAcceptEncodingHeader(this ISpaBuilder spaBuilder)
    {
        spaBuilder.ApplicationBuilder.Use(async (context, next) =>
        {
            context.Request.Headers.AcceptEncoding = string.Empty;

            await next.Invoke(context);
        });
    }
}
