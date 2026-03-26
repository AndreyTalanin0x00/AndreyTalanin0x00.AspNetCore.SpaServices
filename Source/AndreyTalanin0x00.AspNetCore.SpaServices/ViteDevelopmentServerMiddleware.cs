// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the original file: https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/Spa/SpaServices.Extensions/src/ReactDevelopmentServer/ReactDevelopmentServerMiddleware.cs
// This file represents a modified version of the ReactDevelopmentServerMiddleware class.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AndreyTalanin0x00.AspNetCore.SpaServices;

/// <summary>
/// Represents a static middleware that proxies requests to the Vite development server and manages its lifetime.
/// </summary>
internal static class ViteDevelopmentServerMiddleware
{
    private const string c_loggerCategoryName = "Microsoft.AspNetCore.SpaServices";

    private static readonly TimeSpan s_regexMatchTimeout = TimeSpan.FromSeconds(5);
    private static readonly Regex s_spaReadyMessageRegex = new(
        "^\\s*VITE\\sv\\d+(\\.\\d+)*(-[a-zA-Z\\d]*)?\\s+ready\\sin\\s\\d+\\.?\\d+\\s(s|ms)\\s*$",
        RegexOptions.None,
        s_regexMatchTimeout);

    public static void Attach(ISpaBuilder spaBuilder, string scriptName, string[] scriptArgs)
    {
        string spaSourcePath = spaBuilder.Options.SourcePath ?? string.Empty;
        string spaPackageManagerCommand = spaBuilder.Options.PackageManagerCommand;
        bool spaDevServerStrictPort = true;
        int spaDevServerPort = spaBuilder.Options.DevServerPort;

        ArgumentNullException.ThrowIfNull(spaBuilder, nameof(spaBuilder));

        if (string.IsNullOrEmpty(scriptName))
            throw new ArgumentException("Script name can not be null or empty.", nameof(scriptName));

        if (string.IsNullOrEmpty(spaSourcePath))
            throw new ArgumentException($"Property value '{nameof(SpaOptions.SourcePath)}' can not be null or empty.", nameof(spaBuilder));

        if (string.IsNullOrEmpty(spaPackageManagerCommand))
            throw new ArgumentException($"Property value '{nameof(SpaOptions.PackageManagerCommand)}' can not be null or empty.", nameof(spaBuilder));

        // Start the Vite development server and attach to the middleware pipeline.
        IApplicationBuilder applicationBuilder = spaBuilder.ApplicationBuilder;
        IServiceProvider serviceProvider = applicationBuilder.ApplicationServices;
        IHostApplicationLifetime hostApplicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
        DiagnosticSource diagnosticSource = serviceProvider.GetRequiredService<DiagnosticSource>();
        CancellationToken applicationStoppingToken = hostApplicationLifetime.ApplicationStopping;

        ILogger logger = CreateSpaServicesLogger(applicationBuilder, c_loggerCategoryName);

        Task<int> startViteDevelopmentServerTask = StartViteDevelopmentServerAsync(
            scriptName,
            scriptArgs,
            spaSourcePath,
            spaPackageManagerCommand,
            spaDevServerStrictPort,
            spaDevServerPort,
            logger,
            diagnosticSource,
            applicationStoppingToken);

        spaBuilder.UseProxyToSpaDevelopmentServer(async () =>
        {
            TimeSpan startupTimeout = spaBuilder.Options.StartupTimeout;
            string startupTimeoutMessage =
                $"The Vite development server did not start listening for requests within the timeout period of {startupTimeout.TotalSeconds} seconds." + " "
                + "Check the log output for error information.";

            // On each request, we create a separate startup task with its own timeout.
            // That way, even if the first request times out, subsequent requests could still work.
            int port = startViteDevelopmentServerTask != await Task.WhenAny(startViteDevelopmentServerTask, Task.Delay(startupTimeout))
                ? throw new TimeoutException(startupTimeoutMessage)
                : startViteDevelopmentServerTask.Result;

            // Everything we proxy is hardcoded to target http://localhost/ because:
            // - The requests are always from the local machine (we're not accepting remote requests that go directly to the Vite development server).
            // - There's no reason to use HTTPS, and we couldn't even if we wanted to, because in general the Vite development server has no certificate.
            return new UriBuilder("http", "localhost", port).Uri;
        });
    }

    private static async Task<int> StartViteDevelopmentServerAsync(
        string scriptName,
        string[] scriptArgs,
        string spaSourcePath,
        string spaPackageManagerCommand,
        bool spaDevServerStrictPort,
        int spaDevServerPort,
        ILogger logger,
        DiagnosticSource diagnosticSource,
        CancellationToken applicationStoppingToken)
    {
        if (spaDevServerPort == default(int))
            spaDevServerPort = GetAvailablePort();

        logger.LogInformation("Starting the Vite development server on port {spaDevServerPort}.", spaDevServerPort);

        const bool openBrowser = false;
        Dictionary<string, string> environmentVariables = new()
        {
            ["PORT"] = spaDevServerPort.ToString(CultureInfo.InvariantCulture),
            ["STRICT_PORT"] = spaDevServerStrictPort.ToString(CultureInfo.InvariantCulture),
            ["OPEN"] = openBrowser.ToString(CultureInfo.InvariantCulture),
        };

        NodeScriptRunner nodeScriptRunner =
            CreateNodeScriptRunner(scriptName, scriptArgs, spaSourcePath, spaPackageManagerCommand, environmentVariables, diagnosticSource, applicationStoppingToken);

        nodeScriptRunner.AttachToLogger(logger);

        using (EventedStreamStringReader standardOutReader = new(nodeScriptRunner.StandardOut))
        using (EventedStreamStringReader standardErrorReader = new(nodeScriptRunner.StandardError))
        {
            try
            {
                // Although the Vite development server may eventually tell us the URL it's listening on,
                // it doesn't do so until it's finished compiling, and even then only if there were no compiler warnings.
                // So instead of waiting for that, consider it ready as soon as it starts listening for requests.
                await standardOutReader.WaitForAsyncMatch(s_spaReadyMessageRegex);
            }
            catch (EndOfStreamException exception)
            {
                string exceptionMessage =
                    $"The {spaPackageManagerCommand} script '{scriptName}' exited without indicating that the Vite development server was listening for requests." + " "
                    + $"The error output was: '{standardErrorReader.ReadAllText()}'.";

                throw new InvalidOperationException(exceptionMessage, exception);
            }
        }

        return spaDevServerPort;
    }

    private static ILogger CreateSpaServicesLogger(IApplicationBuilder applicationBuilder, string categoryName)
    {
        ILoggerFactory? loggerFactory = applicationBuilder.ApplicationServices.GetService<ILoggerFactory>();
        ILogger logger = loggerFactory?.CreateLogger(categoryName) ?? NullLogger.Instance;
        return logger;
    }

    private static NodeScriptRunner CreateNodeScriptRunner(
        string scriptName,
        string[] scriptArgs,
        string spaSourcePath,
        string spaPackageManagerCommand,
        Dictionary<string, string> environmentVariables,
        DiagnosticSource diagnosticSource,
        CancellationToken applicationStoppingToken)
    {
        return new NodeScriptRunner(
            scriptName,
            scriptArgs,
            spaSourcePath,
            spaPackageManagerCommand,
            environmentVariables,
            diagnosticSource,
            applicationStoppingToken);
    }

    private static int GetAvailablePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();

        EndPoint localEndpoint = listener.LocalEndpoint;
        IPEndPoint ipLocalEndpoint = (IPEndPoint)localEndpoint;

        return ipLocalEndpoint.Port;
    }
}
