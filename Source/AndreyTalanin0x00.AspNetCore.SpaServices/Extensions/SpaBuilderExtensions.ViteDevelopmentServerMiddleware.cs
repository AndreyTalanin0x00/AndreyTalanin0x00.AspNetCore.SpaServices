// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the original file: https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/Spa/SpaServices.Extensions/src/ReactDevelopmentServer/ReactDevelopmentServerMiddlewareExtensions.cs
// This file represents a modified version of the ReactDevelopmentServerMiddlewareExtensions class.

using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices;

namespace AndreyTalanin0x00.AspNetCore.SpaServices.Extensions;

/// <summary>
/// Provides a set of extension methods for the <see cref="ISpaBuilder" /> <see langword="interface" /> for enabling Vite development server middleware support.
/// </summary>
public static class ViteDevelopmentServerMiddlewareSpaBuilderExtensions
{
    /// <summary>
    /// Handles requests by passing them through to an instance of the Vite development server.
    /// This means you can always serve up-to-date CLI-built resources without having to run the Vite development server manually.
    /// </summary>
    /// <param name="spaBuilder">The Single Page Application (SPA) builder.</param>
    /// <param name="npmScript">The name of the script in your package.json file that launches the Vite development server.</param>
    /// <param name="npmScriptArgs">An array of additional parameters for the script.</param>
    /// <remarks>
    /// This feature should only be used in development.
    /// For production deployments, be sure not to enable the Vite development server.
    /// </remarks>
    public static void UseViteDevelopmentServer(this ISpaBuilder spaBuilder, string npmScript, string[]? npmScriptArgs = null)
    {
        SpaOptions spaOptions = spaBuilder.Options;

        ArgumentNullException.ThrowIfNull(spaBuilder, nameof(spaBuilder));

        if (string.IsNullOrEmpty(spaOptions.SourcePath))
        {
            string exceptionMessage = $"To use the {nameof(UseViteDevelopmentServer)} extension method," + " "
                + $"you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} options" + " "
                + $"when calling the {nameof(SpaApplicationBuilderExtensions.UseSpa)} extension method.";

            throw new InvalidOperationException(exceptionMessage);
        }

        ViteDevelopmentServerMiddleware.Attach(spaBuilder, npmScript, npmScriptArgs ?? []);
    }
}
