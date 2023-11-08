using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Security.Cryptography;

namespace OracleSignalProcessor.AlertListener;

internal sealed class OracleAlertListenerFactory : IOracleAlertListenerFactory
{
    private readonly ILogger<OracleAlertListener> _logger;

    public OracleAlertListenerFactory(ILogger<OracleAlertListener> logger)
    {
        _logger = logger;
    }

    public IOracleAlertListener CreateListener(string connectionString, string signalName)
    {
        return new OracleAlertListener(
            connectionFactory: () => new OracleConnection(connectionString),
            uniqueStopSignalName: CreateUniqueStopSignalName(),
            signalName: signalName,
            _logger);
    }

    private static string CreateUniqueStopSignalName()
    {
        var randomBytes = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return $"STOP_{BitConverter.ToString(randomBytes).Replace("-", string.Empty)}";
    }
}

public interface IOracleAlertListenerFactory
{
    IOracleAlertListener CreateListener(string connectionString, string signalName);
}