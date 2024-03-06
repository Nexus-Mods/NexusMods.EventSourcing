﻿using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Algorithms;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class PackedIndexNode : AIndexNode
{
    private readonly IColumn<int> _childCounts;
    private readonly IColumn<int> _childOffsets;
    private readonly List<IDataNode> _children;

    public PackedIndexNode(int length,
        IColumn<EntityId> entityIds,
        IColumn<AttributeId> attributeIds,
        IColumn<TxId> transactionIds,
        IColumn<DatomFlags> flags,
        IBlobColumn values,
        IColumn<int> childCounts,
        IColumn<int> childOffsets,
        IDatomComparator comparator,
        List<IDataNode> children)
    {
        Length = length;
        EntityIds = entityIds;
        AttributeIds = attributeIds;
        TransactionIds = transactionIds;
        Flags = flags;
        Values = values;
        _children = children;
        _childCounts = childCounts;
        _childOffsets = childOffsets;
        Comparator = comparator;
    }

    public override IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < _children.Count; i++)
        {
            foreach (var datom in _children[i])
            {
                yield return datom;
            }
        }
    }

    public override int Length { get; }
    public override IColumn<EntityId> EntityIds { get; }
    public override IColumn<AttributeId> AttributeIds { get; }
    public override IColumn<TxId> TransactionIds { get; }
    public override IColumn<DatomFlags> Flags { get; }
    public override IBlobColumn Values { get; }

    public override Datom this[int idx]
    {
        get
        {
            var acc = 0;
            for (var j = 0; j < _children.Count; j++)
            {
                var childSize = _childCounts[j];
                if (idx < acc + _childCounts[j])
                {
                    return _children[j][idx - acc];
                }
                acc += childSize;
            }
            throw new IndexOutOfRangeException();
        }
    }

    public override Datom LastDatom => throw new NotImplementedException();
    public override void WriteTo<TWriter>(TWriter writer)
    {
        writer.WriteFourCC(FourCC.PackedIndex);
        writer.Write(Length);
        writer.Write(_childCounts.Length);
        EntityIds.WriteTo(writer);
        AttributeIds.WriteTo(writer);
        TransactionIds.WriteTo(writer);
        Flags.WriteTo(writer);
        Values.WriteTo(writer);
        _childCounts.WriteTo(writer);
        _childOffsets.WriteTo(writer);
        writer.Write((byte)Comparator.SortOrder);
        foreach (var child in _children)
        {
            if (child is ReferenceNode indexChunk)
            {
                writer.WriteFourCC(FourCC.ReferenceIndex);
                writer.Write((ulong)indexChunk.Key);
            }
            else if (child is ReferenceNode dataChunk)
            {
                writer.WriteFourCC(FourCC.ReferenceData);
                writer.Write((ulong)dataChunk.Key);
            }
            else
            {
                throw new NotSupportedException("Unknown child type: " + child.GetType().Name);
            }
        }
    }


    public static PackedIndexNode ReadFrom(ref BufferReader reader, NodeStore nodeStore, AttributeRegistry registry)
    {
        var length = reader.Read<int>();
        var childCount = reader.Read<int>();
        var entityIds = ColumnReader.ReadColumn<EntityId>(ref reader, childCount - 1);
        var attributeIds = ColumnReader.ReadColumn<AttributeId>(ref reader, childCount - 1);
        var transactionIds = ColumnReader.ReadColumn<TxId>(ref reader, childCount - 1);
        var flags = ColumnReader.ReadColumn<DatomFlags>(ref reader, childCount - 1);
        var values = ColumnReader.ReadBlobColumn(ref reader, childCount - 1);
        var childCounts = ColumnReader.ReadColumn<int>(ref reader, childCount);
        var childOffsets = ColumnReader.ReadColumn<int>(ref reader, childCount);
        var sortOrder = (SortOrders)reader.Read<byte>();
        var comparator = registry.CreateComparator(sortOrder);

        var children = new List<IDataNode>();
        for (var i = 0; i < childCount; i++)
        {
            var fourcc = reader.ReadFourCC();
            var key = reader.Read<ulong>();
            if (fourcc == FourCC.ReferenceIndex)
            {
                children.Add(new ReferenceNode(nodeStore, StoreKey.From(key), null));
            }
            else if (fourcc == FourCC.ReferenceData)
            {
                children.Add(new ReferenceNode(nodeStore, StoreKey.From(key), null));
            }
            else
            {
                throw new NotSupportedException("Unknown child type: " + fourcc);
            }
        }

        return new PackedIndexNode(length, entityIds, attributeIds, transactionIds, flags, values, childCounts, childOffsets, comparator, children);
    }

    public override IDataNode Flush(INodeStore store)
    {
        for (var i = 0; i < _children.Count; i++)
        {
            _children[i] = _children[i].Flush(store);
        }

        return this;
    }

    public override IEnumerable<IDataNode> Children => _children;

    public override IColumn<int> ChildCounts => _childCounts;
    public override IColumn<int> ChildOffsets => _childOffsets;

    public override IDatomComparator Comparator { get; }

    public override IDataNode ChildAt(int idx)
    {
        return _children[idx];
    }
}
