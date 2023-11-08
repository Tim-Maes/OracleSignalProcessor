namespace OracleSignalProcessor.Events;

public delegate void ErrorOccurredEvent(Exception exception);

public delegate void ReconnectingEvent();

public delegate void SignalReceivedEvent(string name, string message);
