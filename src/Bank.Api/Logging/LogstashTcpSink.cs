using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;

namespace Bank.Api.Logging;

/// <summary>
/// Fire-and-forget JSON lines to Logstash TCP. Drops events if ELK is down so Bank stays up.
/// </summary>
public sealed class LogstashTcpSink : ILogEventSink, IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly ITextFormatter _formatter = new CompactJsonFormatter();
    private readonly BlockingCollection<string> _queue = new(boundedCapacity: 1000);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;

    public LogstashTcpSink(string host, int port)
    {
        _host = host;
        _port = port;
        _worker = Task.Run(PumpAsync);
    }

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        _queue.TryAdd(writer.ToString());
    }

    private async Task PumpAsync()
    {
        TcpClient? client = null;
        StreamWriter? writer = null;
        try
        {
            foreach (var line in _queue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    if (client is not { Connected: true } || writer is null)
                    {
                        writer?.Dispose();
                        client?.Dispose();
                        client = new TcpClient();
                        await client.ConnectAsync(_host, _port, _cts.Token);
                        writer = new StreamWriter(client.GetStream(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
                        {
                            AutoFlush = true,
                            NewLine = "\n"
                        };
                    }

                    await writer.WriteLineAsync(line.TrimEnd());
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    writer?.Dispose();
                    client?.Dispose();
                    writer = null;
                    client = null;
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            writer?.Dispose();
            client?.Dispose();
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _queue.CompleteAdding();
        try
        {
            _worker.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // shutdown: leftover logs are dropped
        }

        _cts.Dispose();
        _queue.Dispose();
    }
}

public static class LogstashTcpSinkExtensions
{
    public static LoggerConfiguration LogstashTcp(
        this LoggerSinkConfiguration writeTo,
        string host,
        int port)
    {
        return writeTo.Sink(new LogstashTcpSink(host, port));
    }
}

