using System;

using AndreyTalanin0x00.AspNetCore.SpaServices.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AndreyTalanin0x00.AspNetCore.SpaServices.Extensions;

/// <summary>
/// Provides a set of extension methods for the <see cref="IApplicationBuilder" /> <see langword="interface" />.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the application to use the React development server (provided by Vite) for the Single Page Application (SPA).
    /// </summary>
    /// <param name="applicationBuilder">The request pipeline builder.</param>
    public static void UseViteSpa(this IApplicationBuilder applicationBuilder)
    {
        IServiceProvider serviceProvider = applicationBuilder.ApplicationServices;

        IWebHostEnvironment webHostEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
        IOptions<SpaDevelopmentServerOptions> spaDevelopmentServerOptionsWrapper = serviceProvider.GetRequiredService<IOptions<SpaDevelopmentServerOptions>>();
        SpaDevelopmentServerOptions spaDevelopmentServerOptions = spaDevelopmentServerOptionsWrapper.Value;

        applicationBuilder.UseSpa(spaBuilder =>
        {
            spaBuilder.Options.SourcePath = spaDevelopmentServerOptions.SourcePath;
            spaBuilder.Options.PackageManagerCommand = spaDevelopmentServerOptions.PackageManagerCommand;

            if (webHostEnvironment.IsDevelopment())
            {
                spaBuilder.UseEmptyAcceptEncodingHeader();

                if (spaDevelopmentServerOptions.UseExternalDevelopmentServer)
                    spaBuilder.UseProxyToSpaDevelopmentServer(spaDevelopmentServerOptions.ExternalDevelopmentServerBaseUri);
                else
                    spaBuilder.UseViteDevelopmentServer(spaDevelopmentServerOptions.Script, spaDevelopmentServerOptions.ScriptParameters);
            }
        });
    }
}
