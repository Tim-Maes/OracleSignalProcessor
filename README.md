# OracleSignalProcessor

Example project for listening to Oracle database alerts and processing DBMS signals.

## Usage

Create a service that implements the IOracleSignalSignalProcessor interface

```
    public class ExampleProcessor : IOracleSignalProcessor
    {
        private readonly ILogger<ExampleListener> _logger;

        public ExampleListener(ILogger<ExampleListener> _logger)
        {
            _logger = _logger;
        }

        public void ProcessSignal(string name, string message)
        {
            _logger.LogInformation($"Received signal {name} with message: {message}")
        }

        public void OnError(Exception exception)
        {
            _logger.LogError(exception.Message);
        }

        public void OnReconnect()
        {
            _logger.LogInformation("Reconnecting...");
        }
    }
```

Register your service
```
    services.AddOracleSignalProcessor<ExampleProcessor>(options => 
    {
        options.ConnectionString = "YourConnectionString";
        options.SignalName = "YourSignalName";
    });
```