// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the original file: https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/Spa/SpaServices.Extensions/src/Util/EventedStreamStringReader.cs
// This file represents a modified version of the EventedStreamStringReader class.

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AndreyTalanin0x00.AspNetCore.SpaServices;

/// <summary>
/// Captures the completed-line notifications from a <see cref="EventedStreamReader"/>, combining the data into a single <see cref="string"/>.
/// Removes ANSI colors from the lines it receives.
/// </summary>
internal class EventedStreamStringReader : IDisposable
{
    private static readonly Regex s_colorEscapeSequencesRegex = new(
        "([\u001b\u009b][[()#;?]*(?:[0-9]{1,4}(?:;[0-9]{0,4})*)?[0-9A-ORZcf-nqry=><])*",
        RegexOptions.None,
        TimeSpan.FromSeconds(1));

    private readonly StringBuilder m_stringBuilder;
    private readonly EventedStreamReader m_eventedStreamReader;
    private bool m_disposed;

    public EventedStreamStringReader(EventedStreamReader eventedStreamReader)
    {
        m_stringBuilder = new StringBuilder();
        m_eventedStreamReader = eventedStreamReader
            ?? throw new ArgumentNullException(nameof(eventedStreamReader));
        m_disposed = false;

        m_eventedStreamReader.LineReceived += OnLineReceived;
        m_eventedStreamReader.StreamClosed += OnStreamClosed;
    }

    public event LineReceivedEventHandler? LineReceived;
    public event StreamClosedEventHandler? StreamClosed;

    public string ReadAllText()
    {
        return m_stringBuilder.ToString();
    }

    public Task<Match> WaitForAsyncMatch(Regex regex)
    {
        TaskCompletionSource<Match> taskCompletionSource = new();
        object taskCompletionSourceLock = new();

        LineReceivedEventHandler? onLineReceived = null;
        StreamClosedEventHandler? onStreamClosed = null;

        void ResolveIfStillPending(Action onResolved)
        {
            lock (taskCompletionSourceLock)
            {
                if (!taskCompletionSource.Task.IsCompleted)
                {
                    LineReceived -= onLineReceived;
                    StreamClosed -= onStreamClosed;

                    onResolved();
                }
            }
        }

        onLineReceived = (line) =>
        {
            Match match = regex.Match(line);
            if (!match.Success)
                return;

            ResolveIfStillPending(() => taskCompletionSource.SetResult(match));
        };

        onStreamClosed = () =>
        {
            ResolveIfStillPending(() => taskCompletionSource.SetException(new EndOfStreamException()));
        };

        LineReceived += onLineReceived;
        StreamClosed += onStreamClosed;

        return taskCompletionSource.Task;
    }

    public void Dispose()
    {
        if (!m_disposed)
        {
            m_eventedStreamReader.LineReceived -= OnLineReceived;
            m_eventedStreamReader.StreamClosed -= OnStreamClosed;

            m_disposed = true;
        }
    }

    private static string SanitizeColorEscapeSequences(string value)
    {
        return s_colorEscapeSequencesRegex.Replace(value, string.Empty);
    }

    private void OnLineReceived(string line)
    {
        line = SanitizeColorEscapeSequences(line);
        m_stringBuilder.AppendLine(line);

        LineReceived?.Invoke(line);
    }

    private void OnStreamClosed()
    {
        StreamClosed?.Invoke();
    }
}
