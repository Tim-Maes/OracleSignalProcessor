﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OracleSignalProcessor.AlertListener;
using OracleSignalProcessor.Options;
using OracleSignalProcessor.SignalProcessor;

namespace OracleSignalProcessor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseSignalProcessor<TProcessor, TOptions>(
        this IServiceCollection services,
        Action<TOptions> setupAction)
        where TProcessor : DatabaseSignalProcessor
        where TOptions : SignalProcessorOptions, new()
    {
        services.Configure(setupAction);
        services.AddSingleton<IOracleAlertListenerFactory, OracleAlertListenerFactory>();
        services.AddSingleton<DatabaseSignalProcessor, TProcessor>();

        services.AddHostedService(provider => (IHostedService)provider.GetRequiredService<DatabaseSignalProcessor>());

        return services;
    }
}