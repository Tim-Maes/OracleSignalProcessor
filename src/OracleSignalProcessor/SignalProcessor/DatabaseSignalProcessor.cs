using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _scopeFactory;

    private bool _isInitialized = false;

    protected DatabaseSignalProcessor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Dispose()
    {
        using var scope = _scopeFactory.CreateScope();
        var listenerFactory = scope.ServiceProvider.GetRequiredService<IOracleAlertListenerFactory>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SignalProcessorOptions>>();

        _listener = listenerFactory.CreateListener(options.Value.ConnectionString, options.Value.SignalName);

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
