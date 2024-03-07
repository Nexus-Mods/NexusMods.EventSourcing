﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage;

/// <summary>
/// In-memory key-value store.
/// </summary>
public class InMemoryKvStore : IKvStore
{
    private readonly ConcurrentDictionary<StoreKey, (int, Memory<byte>)> _store = new();

    public int Size => _store.Values.Sum(v => v.Item2.Length);

    public void Put(StoreKey key, ReadOnlySpan<byte> value)
    {
        _store[key] = (value.Length, value.ToArray());
    }

    public bool TryGet(StoreKey key, out ReadOnlySpan<byte> value)
    {
        if (_store.TryGetValue(key, out var memory))
        {
            value = memory.Item2.Span;
            return true;
        }

        value = default;
        return false;
    }

    public void Delete(StoreKey key)
    {
        _store.TryRemove(key, out _);
    }

    public bool TryGetLatestTx(out TxId key)
    {
        var max = _store.Keys
            .Select(k => k.Value)
            .Where(k => Ids.GetPartition(k) == Ids.Partition.TxLog)
            .OrderDescending()
            .FirstOrDefault();

        if (max == default)
        {
            key = default;
            return false;
        }

        key = TxId.From(max);
        return true;

    }

    public void Dispose()
    {
    }
}
