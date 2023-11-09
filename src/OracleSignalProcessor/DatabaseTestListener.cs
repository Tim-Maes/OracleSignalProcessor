using Microsoft.Extensions.DependencyInjection;
using OracleSignalProcessor.SignalProcessor;

namespace OracleSignalProcessor
{
    internal class DatabaseTestListener : DatabaseSignalProcessor
    {
        ///TODO: Remove the need for this constructor.
        public DatabaseTestListener(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

        protected override void ErrorOccurred(Exception exception)
        {
            throw new NotImplementedException();
        }

        protected override void ProcessSignal(string name, string message)
        {
            throw new NotImplementedException();
        }

        protected override void Reconnecting()
        {
            throw new NotImplementedException();
        }
    }
}
