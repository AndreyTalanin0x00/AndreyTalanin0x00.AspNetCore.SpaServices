// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the original file: https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/Spa/SpaServices.Extensions/src/Npm/NodeScriptRunner.cs
// This file represents a modified version of the NodeScriptRunner class.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Microsoft.Extensions.Logging;

// Disable the IDE0079 (Remove unnecessary suppression) notification (false positive).
#pragma warning disable IDE0079

// Disable the IDE0200 (Lambda expression can be removed) notification for better readability.
#pragma warning disable IDE0200

// Disable the CA2254 (Template should be a static expression) notification due to the logs being received as is from an external source.
#pragma warning disable CA2254

namespace AndreyTalanin0x00.AspNetCore.SpaServices;

/// <summary>
/// Executes the <c>script</c> entries defined in a <c>package.json</c> file, capturing any output written to standard out/error streams.
/// </summary>
internal class NodeScriptRunner : IDisposable
{
    private readonly Process m_npmProcess;
    private readonly DiagnosticSource m_diagnosticSource;
    private readonly CancellationToken m_applicationStoppingToken;
    private bool m_disposed;

    public EventedStreamReader StandardOut { get; }

    public EventedStreamReader StandardError { get; }

    public NodeScriptRunner(
        string scriptName,
        string[] scriptArgs,
        string spaSourcePath,
        string spaPackageManagerCommand,
        IDictionary<string, string>? environmentVariables,
        DiagnosticSource diagnosticSource,
        CancellationToken applicationStoppingToken)
    {
        if (string.IsNullOrEmpty(scriptName))
            throw new ArgumentException("Cannot be null or empty.", nameof(scriptName));

        ArgumentNullException.ThrowIfNull(scriptArgs, nameof(scriptArgs));

        if (string.IsNullOrEmpty(spaSourcePath))
            throw new ArgumentException("Cannot be null or empty.", nameof(spaSourcePath));

        if (string.IsNullOrEmpty(spaPackageManagerCommand))
            throw new ArgumentException("Cannot be null or empty.", nameof(spaPackageManagerCommand));

        ArgumentNullException.ThrowIfNull(diagnosticSource, nameof(diagnosticSource));

        m_diagnosticSource = diagnosticSource;
        m_applicationStoppingToken = applicationStoppingToken;
        m_disposed = false;

        string program = spaPackageManagerCommand;
        string arguments = $"run {scriptName} -- {string.Join(" ", scriptArgs)}";
        if (OperatingSystem.IsWindows())
        {
            program = "cmd";
            arguments = $"/c {spaPackageManagerCommand} {arguments}";
            // On Windows, the node executable is a .cmd file, so it can't be executed directly
            // (except with UseShellExecute=true, but that's no good, because it prevents capturing standard out/error streams).
            // So we need to invoke it via "cmd /c".
        }

        ProcessStartInfo processStartInfo = new(program)
        {
            Arguments = arguments,
            WorkingDirectory = spaSourcePath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (environmentVariables != null)
        {
            foreach (KeyValuePair<string, string> keyValuePair in environmentVariables)
            {
                processStartInfo.Environment[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        m_npmProcess = LaunchNodeProcess(processStartInfo, spaPackageManagerCommand);

        applicationStoppingToken.Register(() => Dispose());

        StandardOut = new EventedStreamReader(m_npmProcess.StandardOutput);
        StandardError = new EventedStreamReader(m_npmProcess.StandardError);

        if (m_diagnosticSource.IsEnabled("Microsoft.AspNetCore.NodeServices.Npm.NpmStarted"))
        {
            m_diagnosticSource.Write("Microsoft.AspNetCore.NodeServices.Npm.NpmStarted", new
            {
                Process = m_npmProcess,
                ProcessStartInfo = processStartInfo,
            });
        }
    }

    public void AttachToLogger(ILogger logger)
    {
        EventedStreamStringReader standardOutReader = new(StandardOut);
        m_applicationStoppingToken.Register(() => standardOutReader.Dispose());
        standardOutReader.LineReceived += line =>
        {
            if (!string.IsNullOrWhiteSpace(line))
                // When the node task emits complete lines, pass them through to the real logger.
                logger.LogInformation(line);
        };

        EventedStreamStringReader standardErrorReader = new(StandardError);
        m_applicationStoppingToken.Register(() => standardErrorReader.Dispose());
        standardErrorReader.LineReceived += line =>
        {
            if (!string.IsNullOrWhiteSpace(line))
                logger.LogError(line);
        };
    }

    public void Dispose()
    {
        if (!m_disposed)
        {
            if (!m_npmProcess.HasExited)
                m_npmProcess.Kill(entireProcessTree: true);

            if (m_diagnosticSource.IsEnabled("Microsoft.AspNetCore.NodeServices.Npm.NpmStopped"))
            {
                m_diagnosticSource.Write("Microsoft.AspNetCore.NodeServices.Npm.NpmStopped", new
                {
                    Process = m_npmProcess,
                });
            }

            m_disposed = true;
        }
    }

    private static Process LaunchNodeProcess(ProcessStartInfo startInfo, string commandName)
    {
        try
        {
            Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("No Node.js process has started.");

            process.EnableRaisingEvents = true;

            return process;
        }
        catch (Exception exception)
        {
            string exceptionMessage =
                $"Failed to start '{commandName}'. To resolve this:\n"
                + $"[1] Ensure that '{commandName}' is installed and can be found in one of the PATH directories.\n"
                + $"    Current PATH environment variable is: {Environment.GetEnvironmentVariable("PATH")}.\n"
                + $"    Make sure the executable is in one of those directories, or update your PATH.\n\n"
                + $"[2] See the {nameof(Exception.InnerException)} for further details of the cause.";

            throw new InvalidOperationException(exceptionMessage, exception);
        }
    }
}
