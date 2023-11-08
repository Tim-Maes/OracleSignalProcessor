using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using OracleSignalProcessor.Events;
using System.Data;

namespace OracleSignalProcessor.AlertListener;

internal sealed class OracleAlertListener : IOracleAlertListener
{
    private static readonly TimeSpan DefaultReconnectHoldOffPeriod = TimeSpan.FromSeconds(30);
    private readonly ILogger<IOracleAlertListener> _logger;
    private readonly Func<IDbConnection> _connectionFactory;
    private readonly string _signalName;
    private readonly string _stopSignalName;
    private readonly TimeSpan _reconnectHoldOffPeriod;
    private readonly ManualResetEvent _terminateEvent = new(false);
    private Thread _thread;

    public event SignalReceivedEvent SignalReceived;

    public event ErrorOccurredEvent ErrorOccurred;

    public event ReconnectingEvent Reconnecting;

    internal OracleAlertListener(
        Func<IDbConnection> connectionFactory,
        string uniqueStopSignalName,
        string signalName,
        ILogger<OracleAlertListener> logger,
        TimeSpan? reconnectHoldOffPeriod = default)
    {
        _connectionFactory = connectionFactory;
        _signalName = signalName;
        _stopSignalName = uniqueStopSignalName;
        _reconnectHoldOffPeriod = reconnectHoldOffPeriod ?? DefaultReconnectHoldOffPeriod;
        _logger = logger;
    }

    public void Dispose()
    {
        _terminateEvent.Set();

        if (_thread == null)
            return;

        try
        {
            SendStopSignal();
        }
        catch (Exception exception)
        {
            ErrorOccurred?.Invoke(exception);
        }

        _thread.Join();
    }

    public void Start()
    {
        if (_thread != null)
            throw new InvalidOperationException("Listener already started");

        _thread = new Thread(Execute);

        _thread.Start();
    }

    private void Execute()
    {
        bool connectingForFirstTime = true;

        while (!_terminateEvent.WaitOne(0, false))
        {
            if (!connectingForFirstTime)
                Reconnecting?.Invoke();
            connectingForFirstTime = false;

            DateTime connectingStartedAtUtc = DateTime.UtcNow;

            try
            {
                WaitForAlerts();
            }
            catch (Exception exception)
            {
                ErrorOccurred?.Invoke(exception);
            }

            TimeSpan remainingHoldOffPeriod = _reconnectHoldOffPeriod - (DateTime.UtcNow - connectingStartedAtUtc);

            if (remainingHoldOffPeriod.TotalMilliseconds > 0)
                _terminateEvent.WaitOne(remainingHoldOffPeriod);
        }
    }

    private void WaitForAlerts()
    {
        using IDbConnection connection = _connectionFactory();
        connection.Open();
        RegisterSignal(connection, _signalName);

        using IDbCommand waitAnyCommand = connection.CreateCommand();
        var nameParameter = new OracleParameter("NAME", OracleDbType.Varchar2, ParameterDirection.Output) { Size = 1000 };
        var messageParameter = new OracleParameter("MESSAGE", OracleDbType.Varchar2, ParameterDirection.Output) { Size = 1000 };
        var statusParameter = new OracleParameter("STATUS", OracleDbType.Int32, ParameterDirection.Output);
        var timeoutParameter = new OracleParameter("TIMEOUT", 30);

        waitAnyCommand.CommandText = "BEGIN SYS.DBMS_ALERT.WAITANY(:NAME, :MESSAGE, :STATUS, :TIMEOUT); END;";
        waitAnyCommand.Parameters.Add(nameParameter);
        waitAnyCommand.Parameters.Add(messageParameter);
        waitAnyCommand.Parameters.Add(statusParameter);
        waitAnyCommand.Parameters.Add(timeoutParameter);

        while (!_terminateEvent.WaitOne(0, false))
        {
            waitAnyCommand.ExecuteNonQuery();

            if ((OracleDecimal)statusParameter.Value != 0)
            {
                continue;
            }

            string name = ((OracleString)nameParameter.Value).Value;
            string message = !((OracleString)messageParameter.Value).IsNull ? ((OracleString)messageParameter.Value).Value : null;
            if (name == _stopSignalName)
                break;

            try
            {
                SignalReceived?.Invoke(name, message);
            }
            catch (Exception exception)
            {
                ErrorOccurred?.Invoke(exception);
            }
        }

        UnregisterSignals(connection);
    }

    private static void RegisterSignal(IDbConnection connection, string signalName)
    {
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = "BEGIN SYS.DBMS_ALERT.REGISTER(:SIGNAL_NAME); END;";
        command.Parameters.Add(new OracleParameter("SIGNAL_NAME", signalName));
        command.ExecuteNonQuery();
    }

    private static void UnregisterSignals(IDbConnection connection)
    {
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = "BEGIN SYS.DBMS_ALERT.REMOVEALL(); END;";
        command.ExecuteNonQuery();
    }

    private void SendStopSignal()
    {
        using IDbConnection connection = _connectionFactory();
        connection.Open();

        using IDbCommand command = connection.CreateCommand();
        command.CommandText = "BEGIN SYS.DBMS_ALERT.SIGNAL(:SIGNAL_NAME, ''); END;";
        command.Parameters.Add(new OracleParameter("SIGNAL_NAME", _stopSignalName));
        command.ExecuteNonQuery();
    }
}

public interface IOracleAlertListener : IDisposable
{
    void Start();

    event SignalReceivedEvent SignalReceived;

    event ErrorOccurredEvent ErrorOccurred;

    event ReconnectingEvent Reconnecting;
}