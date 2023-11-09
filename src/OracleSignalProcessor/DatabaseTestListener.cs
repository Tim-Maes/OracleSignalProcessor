using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OracleSignalProcessor.AlertListener;
using OracleSignalProcessor.Options;
using OracleSignalProcessor.SignalProcessor;

namespace OracleSignalProcessor
{
    internal class DatabaseTestListener : DatabaseSignalProcessor
    {
        private readonly ILogger<DatabaseTestListener> _logger;

        public DatabaseTestListener(
            IOracleAlertListenerFactory factory,
            IOptions<SignalProcessorOptions> options,
            ILogger<DatabaseTestListener> logger)
            : base(factory, options)
        {
            _logger = logger;
        }

        protected override void ErrorOccurred(Exception exception)
        {
            _logger.LogError($"An error occured: {exception}");
        }

        protected override void ProcessSignal(string name, string message)
        {
            _logger.LogInformation($"Received signal {name} with message: {message}");
        }

        protected override void Reconnecting()
        {
            _logger.LogInformation("Reconnecting signal processor...");
        }
    }
}
