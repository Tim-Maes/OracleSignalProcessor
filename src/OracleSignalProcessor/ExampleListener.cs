using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OracleSignalProcessor.AlertListener;
using OracleSignalProcessor.Options;
using OracleSignalProcessor.SignalProcessor;

namespace OracleSignalProcessor
{
    internal class ExampleListener : DatabaseSignalProcessor
    {
        private readonly ILogger<ExampleListener> _logger;

        public ExampleListener(
            IOracleAlertListenerFactory factory,
            IOptions<SignalProcessorOptions> options,
            ILogger<ExampleListener> logger)
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
