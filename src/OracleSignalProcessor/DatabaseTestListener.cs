using Microsoft.Extensions.Options;
using OracleSignalProcessor.AlertListener;
using OracleSignalProcessor.Options;
using OracleSignalProcessor.SignalProcessor;

namespace OracleSignalProcessor
{
    internal class DatabaseTestListener : DatabaseSignalProcessor
    {
        public DatabaseTestListener(IOracleAlertListenerFactory factory, IOptions<SignalProcessorOptions> options)
            : base(factory, options) { }

        protected override void ErrorOccurred(Exception exception)
        {
            // Do stuff when error occurs
        }

        protected override void ProcessSignal(string name, string message)
        {
            // Do stuff when dbms_signal sends a alert
            // Example: send notification to UI with SignalR
        }

        protected override void Reconnecting()
        {
            // Do stuff on reconnect
        }
    }
}
