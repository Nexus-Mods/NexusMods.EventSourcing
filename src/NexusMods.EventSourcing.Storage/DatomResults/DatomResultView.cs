﻿using System;
using System.Diagnostics;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

namespace NexusMods.EventSourcing.Storage.DatomResults;

/// <summary>
/// A result set of datoms that are a subview over a larger result set.
/// </summary>
public class DatomResultView(IDatomResult src, long offset, long length) : IDatomResult
{
    public static DatomResultView Create(IDatomResult src, long offset, long length)
    {
        Debug.Assert(offset >= 0);
        Debug.Assert(length >= 0);
        Debug.Assert(offset + length <= src.Length);
        return new DatomResultView(src, offset, length);
    }

    public long Length => length;


    public void Fill(long selfOffset, DatomChunk chunk)
    {
        src.Fill(selfOffset + offset, chunk);
    }

    public void FillValue(long offset, DatomChunk chunk, int idx)
    {
        throw new NotImplementedException();
    }

    public EntityId GetEntityId(long idx)
    {
        return src.GetEntityId(idx + offset);
    }

    public AttributeId GetAttributeId(long idx)
    {
        return src.GetAttributeId(idx + offset);
    }

    public TxId GetTransactionId(long idx)
    {
        return src.GetTransactionId(idx + offset);
    }

    public ReadOnlySpan<byte> GetValue(long idx)
    {
        return src.GetValue(idx + offset);
    }

    public ReadOnlyMemory<byte> GetValueMemory(long idx)
    {
        return src.GetValueMemory(idx + offset);
    }

    public override string ToString()
    {
        return this.DatomResultToString();
    }
}
