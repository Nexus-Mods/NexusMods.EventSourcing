﻿using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.Index;

namespace NexusMods.EventSourcing.Storage.Nodes.Index;

public class Appendable : IReadable, IAppendable
{
    private readonly int _length;
    private readonly bool _isFrozen;

    private readonly Columns.ULongColumns.Appendable _entityIds;
    private readonly Columns.ULongColumns.Appendable _attributeIds;
    private readonly Columns.BlobColumns.Appendable _values;
    private readonly Columns.ULongColumns.Appendable _transactionIds;
    private readonly Columns.ULongColumns.Appendable _childCounts;
    private readonly Columns.ULongColumns.Appendable _childOffsets;

    private List<EventSourcing.Abstractions.Nodes.Data.IReadable> _children = new();

    public Appendable(int initialSize = Columns.ULongColumns.Appendable.DefaultSize)
    {
        _isFrozen = false;
        _length = 0;
        _entityIds = Columns.ULongColumns.Appendable.Create(initialSize);
        _attributeIds = Columns.ULongColumns.Appendable.Create(initialSize);
        _transactionIds = Columns.ULongColumns.Appendable.Create(initialSize);
        _values = Columns.BlobColumns.Appendable.Create(initialSize);
        _childCounts = Columns.ULongColumns.Appendable.Create(initialSize);
        _childOffsets = Columns.ULongColumns.Appendable.Create(initialSize);
    }

    public int Length => _length;
    public long DeepLength { get; }

    public Datom this[int idx] => throw new NotImplementedException();

    public Datom LastDatom { get; }

    public EntityId GetEntityId(int idx)
    {
        return EntityId.From(_entityIds[idx]);
    }

    public AttributeId GetAttributeId(int idx)
    {
        return AttributeId.From(_attributeIds[idx]);
    }

    public TxId GetTransactionId(int idx)
    {
        return TxId.From(_transactionIds[idx]);
    }

    public ReadOnlySpan<byte> GetValue(int idx)
    {
        return _values[idx];
    }

    public int FillChunk(int offset, int length, ref DatomChunk chunk)
    {
        throw new NotImplementedException();
    }

    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable EntityIdsColumn => _entityIds;
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable AttributeIdsColumn => _attributeIds;
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable TransactionIdsColumn => _transactionIds;
    public EventSourcing.Abstractions.Columns.BlobColumns.IReadable ValuesColumn => _values;

    public long GetChildCount(int idx)
    {
        return (long)_childCounts[idx];
    }

    public long GetChildOffset(int idx)
    {
        return (long)_childOffsets[idx];
    }

    EventSourcing.Abstractions.Nodes.Data.IReadable IReadable.GetChild(int idx)
    {
        throw new NotImplementedException();
    }

    public NexusMods.EventSourcing.Abstractions.Nodes.Data.IReadable GetChild(int idx)
    {
        return _children[idx];
    }


    public IEnumerator<Datom> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IAppendable Ingest(EventSourcing.Abstractions.Nodes.Data.IReadable data)
    {
        throw new NotImplementedException();
    }
}
