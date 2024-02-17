﻿using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model.Attributes;


public static class Services
{
    public static IServiceCollection AddTestModel(this IServiceCollection services) =>
        services.AddAttribute<ModFileAttributes.Path>()
            .AddAttribute<ModFileAttributes.Hash>()
            .AddAttribute<ModFileAttributes.Index>()
            .AddAttribute<ArchiveFileAttributes.Path>()
            .AddAttribute<ArchiveFileAttributes.ArchiveHash>();
}
