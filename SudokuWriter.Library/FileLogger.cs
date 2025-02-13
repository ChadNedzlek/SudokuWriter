using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace VaettirNet.SudokuWriter.Library;

public class BufferedFileLoggerProvider : ILoggerProvider, IAsyncDisposable, ISupportExternalScope
{
    private readonly Channel<Record> _records = Channel.CreateBounded<Record>(50);
    private readonly Task _backgroundTask;
    private readonly IOptions<Options> _options;
    private readonly TimeProvider _timeProvider;
    private IExternalScopeProvider _scopeProvider;

    public class Options
    {
        public int CountOfLogs { get; set; } = 5;
        public string FilePathPattern { get; set; } = "log_{0}_{1}.txt";
    }

    public BufferedFileLoggerProvider(IOptions<Options> options, TimeProvider timeProvider = null, IExternalScopeProvider scopeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _options = options;
        _scopeProvider = scopeProvider;
        _backgroundTask = Task.Run(ProcessLogQueue);
        ILogger<BufferedFileLoggerProvider> logger = NullLogger<BufferedFileLoggerProvider>.Instance;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new BufferedFileLogger(this);
    }

    private async Task ProcessLogQueue()
    {
        ChannelReader<Record> reader = _records.Reader;
        await foreach(Record record in reader.ReadAllAsync())
        {
            // Write stuff
        }
    }

    private class Record : BufferedLogRecord
    {
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
        private readonly BufferedFileLoggerProvider _fileLogger;

        public BufferedFileLogger(BufferedFileLoggerProvider fileLogger)
        {
            _fileLogger = fileLogger;
        }

        public IDisposable BeginScope<TState>(TState state) => _fileLogger._scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            
            List<IReadOnlyList<KeyValuePair<string, object>>> attributes = [];
            _fileLogger._scopeProvider.ForEachScope((scope, list) =>
            {
                if (scope is IReadOnlyList<KeyValuePair<string, object>> attributes)
                    list.Add(attributes);
            }, attributes);
            
            Record record;
            if (attributes.Count == 0)
            {
                record = new Record(
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
                Thread.Sleep(10);
            }
        }
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }
}

