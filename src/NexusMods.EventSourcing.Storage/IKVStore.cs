﻿using System;
using NexusMods.EventSourcing.Storage.ValueTypes;

namespace NexusMods.EventSourcing.Storage;

/// <summary>
/// Implements a interface for loading and saving blocks of data.
/// </summary>
public interface IKvStore
{
    public void Put(StoreKey key, ReadOnlySpan<byte> value);

    public bool TryGet(StoreKey key, out ReadOnlySpan<byte> value);

    public void Delete(StoreKey key);
}
