using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace VaettirNet.SudokuWriter.Library;

public sealed class BufferedFileLoggerProvider : ILoggerProvider, IAsyncDisposable, ISupportExternalScope
{
    public class Options
    {
        public int CountOfLogs { get; set; } = 5;
        public string FilePathPattern { get; set; } = "log_{0}_{1}.txt";
    }

    private readonly Channel<Record> _records = Channel.CreateBounded<Record>(500);
    private readonly Task _backgroundTask;
    private readonly IOptions<Options> _options;
    private readonly TimeProvider _timeProvider;
    private IExternalScopeProvider _scopeProvider;
    private bool _closing;

    public BufferedFileLoggerProvider(IOptions<Options> options, TimeProvider timeProvider = null, IExternalScopeProvider scopeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _options = options;
        _scopeProvider = scopeProvider;
        _backgroundTask = Task.Run(ProcessLogQueue);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new BufferedFileLogger(this, categoryName);
    }

    private async Task ProcessLogQueue()
    {
        JsonSerializerOptions o = new()
        {
            WriteIndented = false,
        };
        
        StreamWriter writer = null;
        CsvWriter csv = null;
        ChannelReader<Record> reader = _records.Reader;
        try
        {
            await foreach (Record record in reader.ReadAllAsync())
            {
                EnsureWriter();
                csv.WriteField(record.Timestamp);
                csv.WriteField(record.LogLevel.ToString());
                csv.WriteField(record.CategoryName);
                csv.WriteField(record.EventId);
                csv.WriteField(record.FormattedMessage);
                csv.WriteField(record.Exception);
                csv.WriteField(record.ActivitySpanId);
                csv.WriteField(record.ActivityTraceId);
                csv.WriteField(record.ManagedThreadId);
                csv.WriteField(record.MessageTemplate);
                csv.WriteField(JsonSerializer.Serialize(new Dictionary<string, object>(record.Attributes)));
                await csv.NextRecordAsync();
                await csv.FlushAsync();
            }
        }
        finally
        {
            if (csv != null) await csv.DisposeAsync();
            if (writer != null) await writer.DisposeAsync();
        }

        return;


        void EnsureWriter()
        {
            if (writer != null)
            {
                return;
            }

            string wildcardFullPath = string.Format(_options.Value.FilePathPattern, "*", "*");
            string dir = Path.GetDirectoryName(wildcardFullPath);
            string oldLogFilePattern = Path.GetFileName(wildcardFullPath);
            Directory.CreateDirectory(dir);

            foreach (var oldLogFile in Directory.GetFiles(dir, oldLogFilePattern)
                .OrderByDescending(f => f)
                .Skip(_options.Value.CountOfLogs)
            )
            {
                File.Delete(oldLogFile);
            }

            for(int count = 0; count < 100; count++)
            {
                string path = string.Format(_options.Value.FilePathPattern, _timeProvider.GetLocalNow().ToString("yyyy-MM-ddTHH-mm-ss"), count);
                try
                {
                    writer = new StreamWriter(
                        path,
                        new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.ReadWrite }
                    );
                    csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                    return;
                }
                catch (IOException e) when ((e.HResult & 0xFFFF) == 80)
                {
                    // We are going to try again
                }
            }

            throw new InvalidOperationException("Unable to create log file after 100 attempts");
        }
    }

    private class Record : BufferedLogRecord
    {
        public string CategoryName { get; }
        public override DateTimeOffset Timestamp { get; }
        public override LogLevel LogLevel { get; }
        public override EventId EventId { get; }
        public override string Exception { get; }
        public override ActivitySpanId? ActivitySpanId { get; }
        public override ActivityTraceId? ActivityTraceId { get; }
        public override int? ManagedThreadId { get; }
        public override string FormattedMessage { get; }
        public override string MessageTemplate { get; }
        private IReadOnlyList<IReadOnlyList<KeyValuePair<string, object>>> _attributesList;
        private IReadOnlyList<KeyValuePair<string, object>> _attributes;

        public override IReadOnlyList<KeyValuePair<string, object>> Attributes
        {
            get
            {
                if (_attributes is { } a)
                    return a;
                
                Dictionary<string, object> result = [];
                
                foreach (IReadOnlyList<KeyValuePair<string, object>> attrs in _attributesList)
                foreach ((string key, object value) in attrs)
                    result.Add(key, value);
                return result.ToList();
            }
        }

        public Record(
            string categoryName,
            DateTimeOffset timestamp,
            LogLevel logLevel,
            EventId eventId,
            string formattedMessage,
            string exception = null,
            ActivitySpanId? activitySpanId = default,
            ActivityTraceId? activityTraceId = default,
            int? managedThreadId = default,
            IReadOnlyList<KeyValuePair<string, object>> attributes = null,
            string messageTemplate = null
        )
        {
            CategoryName = categoryName;
            Timestamp = timestamp;
            LogLevel = logLevel;
            EventId = eventId;
            Exception = exception;
            ActivitySpanId = activitySpanId;
            ActivityTraceId = activityTraceId;
            ManagedThreadId = managedThreadId;
            FormattedMessage = formattedMessage;
            MessageTemplate = messageTemplate;
            _attributes = attributes;
        }
        
        public Record(
            string categoryName,
            DateTimeOffset timestamp,
            LogLevel logLevel,
            EventId eventId,
            string formattedMessage,
            string exception = null,
            ActivitySpanId? activitySpanId = default,
            ActivityTraceId? activityTraceId = default,
            int? managedThreadId = default,
            IReadOnlyList<IReadOnlyList<KeyValuePair<string, object>>> attributes = null,
            string messageTemplate = null
        )
        {
            CategoryName = categoryName;
            Timestamp = timestamp;
            LogLevel = logLevel;
            EventId = eventId;
            Exception = exception;
            ActivitySpanId = activitySpanId;
            ActivityTraceId = activityTraceId;
            ManagedThreadId = managedThreadId;
            FormattedMessage = formattedMessage;
            MessageTemplate = messageTemplate;
            _attributesList = attributes;
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        _closing = true;
        try
        {
            _records.Writer.Complete();
            await _backgroundTask;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private class BufferedFileLogger : ILogger
    {
        public string CategoryName { get; }

        private class Null : IDisposable
        {
            public static Null Instance { get; } = new ();

            private Null()
            {
            }

            public void Dispose()
            {
            }
        }
        
        private readonly BufferedFileLoggerProvider _fileLogger;

        public BufferedFileLogger(BufferedFileLoggerProvider fileLogger, string categoryName)
        {
            CategoryName = categoryName;
            _fileLogger = fileLogger;
        }

        public IDisposable BeginScope<TState>(TState state) => _fileLogger._scopeProvider?.Push(state) ?? Null.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (_fileLogger._closing)
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            
            List<IReadOnlyList<KeyValuePair<string, object>>> attributes = [];
            _fileLogger._scopeProvider?.ForEachScope((scope, list) =>
            {
                if (scope is IReadOnlyList<KeyValuePair<string, object>> a)
                    list.Add(a);
            }, attributes);
            
            Record record;
            if (attributes.Count == 0)
            {
                record = new Record(
                    CategoryName,
                    _fileLogger._timeProvider.GetLocalNow(),
                    logLevel,
                    eventId,
                    message,
                    exception?.ToString(),
                    Activity.Current?.SpanId,
                    Activity.Current?.TraceId,
                    Environment.CurrentManagedThreadId,
                    attributes: state as IReadOnlyList<KeyValuePair<string, object>>
                );
            }
            else
            {
                if (state is IReadOnlyList<KeyValuePair<string, object>> a)
                    attributes.Add(a);
                
                record = new Record(
                    CategoryName,
                    _fileLogger._timeProvider.GetLocalNow(),
                    logLevel,
                    eventId,
                    message,
                    exception?.ToString(),
                    Activity.Current?.SpanId,
                    Activity.Current?.TraceId,
                    Environment.CurrentManagedThreadId,
                    attributes: attributes
                );
            }

            while (!_fileLogger._records.Writer.TryWrite(record))
            {
                if (_fileLogger._closing)
                {
                    return;
                }
                
                Thread.Sleep(10);
            }
        }
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }
}

