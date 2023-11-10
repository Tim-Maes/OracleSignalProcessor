using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OracleSignalProcessor.AlertListener;
using OracleSignalProcessor.Options;

namespace OracleSignalProcessor.SignalProcessor;

public abstract class DatabaseSignalProcessor<TProcessor>
    : IHostedService, IDisposable
    where TProcessor : IOracleSignalProcessor
{
    private readonly IOracleAlertListenerFactory _listenerFactory;
    private readonly TProcessor _signalProcessor;
    private readonly string _connectionString;
    private readonly string _signalName;
    private IOracleAlertListener _listener;
    private bool _isInitialized = false;

    protected DatabaseSignalProcessor(
        IOracleAlertListenerFactory listenerFactory,
        TProcessor signalProcessor,
        IOptions<SignalProcessorOptions> options)
    {
        _listenerFactory = listenerFactory;
        _signalProcessor = signalProcessor;
        _signalName = options.Value.SignalName;
        _connectionString = options.Value.ConnectionString;
    }

    private async Task Init()
    {
        _listener = _listenerFactory.CreateListener(_connectionString, _signalName);
        _listener.SignalReceived += ProcessSignal;
        _listener.ErrorOccurred += ErrorOccurred;
        _listener.Reconnecting += Reconnecting;
        _listener.Start();
    }

    public void Dispose()
    {
        if (_listener == null)
            return;

        _listener.SignalReceived -= ProcessSignal;
        _listener.ErrorOccurred -= ErrorOccurred;
        _listener.Reconnecting -= Reconnecting;
        _listener.Dispose();

        GC.SuppressFinalize(this);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        lock (this)
        {
            if (_isInitialized)
                return;
            _isInitialized = true;
        }

        await Init();
    }

    private void ProcessSignal(string name, string message) => _signalProcessor.ProcessSignal(name, message);

    private void ErrorOccurred(Exception exception) => _signalProcessor.OnError(exception);

    private void Reconnecting() => _signalProcessor.OnReconnect();


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            await InitializeAsync();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }
}

public interface IOracleSignalProcessor
{
    void ProcessSignal(string name, string message);

    void OnError(Exception exception);

    void OnReconnect();
}