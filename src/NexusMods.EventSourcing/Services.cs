﻿using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

/// <summary>
///     Extension methods for adding attributes and other types to the service collection.
/// </summary>
public static class Services
{
    /// <summary>
    ///     Registers the event sourcing services with the service collection.
    /// </summary>
    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
    {
        services.AddSingleton<IConnection, Connection>();
        return services;
    }
}
