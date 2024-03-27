﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.DatomIterators;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.Storage;
using NexusMods.EventSourcing.Storage.Abstractions;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing;

internal class Db : IDb
{
    private readonly Connection _connection;
    private readonly TxId _txId;
    private readonly ISnapshot _snapshot;
    private readonly AttributeRegistry _registry;

    public Db(ISnapshot snapshot, Connection connection, TxId txId, AttributeRegistry registry)
    {
        _registry = registry;
        _connection = connection;
        _snapshot = snapshot;
        _txId = txId;
    }

    public TxId BasisTxId => _txId;
    public IConnection Connection => _connection;

    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids) where TModel : IReadModel
    {
        throw new NotImplementedException();

        /*
        var reader = _connection.ModelReflector.GetReader<TModel>();
        foreach (var id in ids)
        {
            var iterator = store.GetAttributesForEntity(id, _txId).GetEnumerator();
            yield return reader(id, iterator, this);
        }*/
    }

    public TModel Get<TModel>(EntityId id) where TModel : IReadModel
    {
        var reader = _connection.ModelReflector.GetReader<TModel>();

        using var source = _snapshot.GetIterator(IndexType.EAVTCurrent);
        using var enumerator = source.SeekTo(id)
            .While(id)
            .Resolve()
            .GetEnumerator();
        return reader(id, enumerator, this);
    }

    /// <inheritdoc />
    public IEnumerable<TModel> GetReverse<TAttribute, TModel>(EntityId id) where TAttribute : IAttribute<EntityId> where TModel : IReadModel
    {
        using var attrSource = _snapshot.GetIterator(IndexType.VAETCurrent);
        var attrId = _registry.GetAttributeId<TAttribute>();
        var eIds = attrSource
            .SeekTo(attrId, id)
            .WhileUnmanagedV(id)
            .While(attrId)
            .Select(c => c.CurrentKeyPrefix().E);

        var reader = _connection.ModelReflector.GetReader<TModel>();
        using var readerSource = _snapshot.GetIterator(IndexType.EAVTCurrent);

        foreach (var e in eIds)
        {
            // Inlining this code so that we can re-use the iterator means roughly a 25% speedup
            // in loading a large number of entities.
            using var enumerator = readerSource
                .SeekTo(e)
                .While(e)
                .Resolve()
                .GetEnumerator();
            yield return reader(e, enumerator, this);
        }
    }

    public void Reload<TOuter>(TOuter aActiveReadModel) where TOuter : IActiveReadModel
    {
        var reader = _connection.ModelReflector.GetActiveReader<TOuter>();/*
        var iterator = store.GetAttributesForEntity(aActiveReadModel.Id, _txId).GetEnumerator();
        reader(aActiveReadModel, iterator);*/
    }

    public IEnumerable<IReadDatom> Datoms(EntityId entityId)
    {
        using var iterator = _snapshot.GetIterator(IndexType.EAVTCurrent);
        foreach (var datom in iterator.SeekTo(entityId)
                     .While(entityId)
                     .Resolve())
            yield return datom;

    }

    public IEnumerable<IReadDatom> Datoms<TAttribute>(IndexType type)
    where TAttribute : IAttribute
    {
        var a = _registry.GetAttributeId<TAttribute>();
        using var iterator = _snapshot.GetIterator(type);
        foreach (var datom in iterator
                     .SeekStart()
                     .While(a)
                     .Resolve())
            yield return datom;
    }

    public void Dispose()
    {
    }
}
