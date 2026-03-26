// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the original file: https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/Spa/SpaServices.Extensions/src/Util/EventedStreamReader.cs
// This file represents a modified version of the EventedStreamReader class.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AndreyTalanin0x00.AspNetCore.SpaServices;

/// <summary>
/// Wraps a <see cref="StreamReader"/> to expose an evented API, issuing notifications when the stream emits partial lines, completed lines, or finally closes.
/// </summary>
internal class EventedStreamReader
{
    private readonly StringBuilder m_lineBuffer;
    private readonly StreamReader m_streamReader;

    public EventedStreamReader(StreamReader streamReader)
    {
        m_lineBuffer = new StringBuilder();
        m_streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));

        Task.Factory.StartNew(Run, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    public event LineReceivedEventHandler? LineReceived;

    public event ChunkReceivedEventHandler? ChunkReceived;

    public event StreamClosedEventHandler? StreamClosed;

    private async Task Run()
    {
        char[] chunkBuffer = new char[8 * 1024];
        while (true)
        {
            int chunkLength = await m_streamReader.ReadAsync(chunkBuffer, 0, chunkBuffer.Length);
            if (chunkLength == 0)
            {
                if (m_lineBuffer.Length > 0)
                {
                    OnLineReceived(m_lineBuffer.ToString());
                    m_lineBuffer.Clear();
                }

                OnStreamClosed();

                break;
            }

            OnChunkReceived(new ArraySegment<char>(chunkBuffer, 0, chunkLength));

            int startPosition = 0;

            int GetFirstLineBreakPosition(int startPosition, int chunkLength)
            {
                return Array.IndexOf(chunkBuffer, '\n', startPosition, chunkLength - startPosition);
            }

            int lineBreakPosition;
            while ((lineBreakPosition = GetFirstLineBreakPosition(startPosition, chunkLength)) >= 0 && startPosition < chunkLength)
            {
                int length = lineBreakPosition + 1 - startPosition;

                // Copy everything up to the next line break.
                m_lineBuffer.Append(chunkBuffer, startPosition, length);

                OnLineReceived(m_lineBuffer.ToString());
                m_lineBuffer.Clear();

                startPosition = lineBreakPosition + 1;
            }

            if (lineBreakPosition < 0 && startPosition < chunkLength)
            {
                // Save everything after the last line break to the buffer.
                m_lineBuffer.Append(chunkBuffer, startPosition, chunkLength - startPosition);
            }
        }
    }

    private void OnLineReceived(string line)
    {
        LineReceived?.Invoke(line);
    }

    private void OnChunkReceived(ArraySegment<char> chunk)
    {
        ChunkReceived?.Invoke(chunk);
    }

    private void OnStreamClosed()
    {
        StreamClosed?.Invoke();
    }
}

internal delegate void LineReceivedEventHandler(string line);

internal delegate void ChunkReceivedEventHandler(ArraySegment<char> chunk);

internal delegate void StreamClosedEventHandler();
