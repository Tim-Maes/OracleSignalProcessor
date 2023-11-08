using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OracleSignalProcessor.AlertListener;
using OracleSignalProcessor.Options;

namespace OracleSignalProcessor.SignalProcessor;

public abstract class DatabaseSignalProcessor : IHostedService, IDisposable
{
    private readonly IOracleAlertListenerFactory _listenerFactory;
    private readonly string _connectionString;
    private readonly string _signalName;
    private IOracleAlertListener _listener;
    private bool _isInitialized = false;

    protected DatabaseSignalProcessor(IOracleAlertListenerFactory listenerFactory, IOptions<SignalProcessorOptions> options)
    {
        _listenerFactory = listenerFactory;
        _signalName = options.Value.SignalName;
        _connectionString = options.Value.ConnectionString;
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

    private async Task Init()
    {
        _listener = _listenerFactory.CreateListener(_connectionString, _signalName);
        _listener.SignalReceived += ProcessSignal;
        _listener.ErrorOccurred += ErrorOccurred;
        _listener.Reconnecting += Reconnecting;
        _listener.Start();
    }

    protected abstract Task<IEnumerable<string>> GetSignalNames();

    protected abstract void ProcessSignal(string name, string message);

    protected abstract void ErrorOccurred(Exception exception);

    protected abstract void Reconnecting();

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
