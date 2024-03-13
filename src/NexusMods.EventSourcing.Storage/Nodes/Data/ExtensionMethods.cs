﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using FlatSharp;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Nodes.Data;

public static class ExtensionMethods
{
    /// <summary>
    /// Returns the indices that would sort the <see cref="IReadable"/> according to the given <see cref="IDatomComparator"/>.
    /// </summary>
    public static int[] GetSortIndices(this IReadable readable, IDatomComparator comparator)
    {
        var pidxs = GC.AllocateUninitializedArray<int>(readable.Length);

        // TODO: may not matter, but we could probably use a vectorized version of this
        for (var i = 0; i < pidxs.Length; i++)
        {
            pidxs[i] = i;
        }

        var comp = comparator.MakeComparer(readable);
        Array.Sort(pidxs, 0, readable.Length, comp);

        return pidxs;
    }

    /// <summary>
    /// Sorts the node using the given comparator and returns a lightweight sorted view of the node.
    /// </summary>
    public static IReadable AsSorted(this IReadable src, IDatomComparator comparator)
    {
        EnsureFrozen(src);
        var indexes = src.GetSortIndices(comparator);
        return new SortedReadable(indexes, src);
    }

    /// <summary>
    /// Creates a new IReadable by creating a read-only view of a portion of the given IReadable.
    /// </summary>
    public static IReadable SubView(this IReadable src, int offset, int length)
    {
        EnsureFrozen(src);
        return new ReadableView(src, offset, length);
    }

    /// <summary>
    /// Splits the node into sub nodes of the given maximum size, attempts to split the nodes into
    /// blocks of size no larger than the given block size, but all of the same size.
    /// </summary>
    public static IEnumerable<IReadable> Split(this IReadable src, int blockSize)
    {
        EnsureFrozen(src);

        var length = src.Length;
        var numBlocks = (length + blockSize - 1) / blockSize;
        var baseBlockSize = length / numBlocks;
        var remainder = length % numBlocks;

        var offset = 0;
        for (var i = 0; i < numBlocks; i++)
        {
            var currentBlockSize = baseBlockSize;
            if (remainder > 0)
            {
                currentBlockSize++;
                remainder--;
            }
            yield return src.SubView(offset, currentBlockSize);
            offset += currentBlockSize;
        }
    }

    /// <summary>
    /// Freezes the given IReadable if it is an IAppendable and not already frozen.
    /// </summary>
    private static void EnsureFrozen(IReadable src)
    {
        if (src is IAppendable { IsFrozen: false } appendable)
        {
            appendable.Freeze();
        }
    }


    private static int FindEATVReader(this IReadable readable, in Datom target, IAttributeRegistry registry)
    {
        var start = 0;
        var end = readable.Length;

        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.E.CompareTo(readable.GetEntityId(mid));
            if (cmp == 0)
            {
                var attrId = readable.GetAttributeId(mid);
                var attrCmp = target.A.CompareTo(attrId);
                if (attrCmp == 0)
                {
                    var tmpCmp = target.T.CompareTo(readable.GetTransactionId(mid));
                    if (tmpCmp == 0)
                    {
                        cmp = registry.CompareValues(attrId, target.V.Span, readable.GetValue(mid));
                    }
                    else
                    {
                        cmp = -tmpCmp;
                    }
                }
                else
                {
                    cmp = attrCmp;
                }
            }

            if (cmp > 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }
        return start;
    }

    private static int FindAETVReader(this IReadable src, in Datom target, IAttributeRegistry registry)
    {
        var start = 0;
        var end = src.Length;

        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.A.CompareTo(src.GetAttributeId(mid));
            if (cmp == 0)
            {
                var entCmp = target.E.CompareTo(src.GetEntityId(mid));
                if (entCmp == 0)
                {
                    var tCmp = -target.T.CompareTo(src.GetTransactionId(mid));
                    if (tCmp == 0)
                    {
                        cmp = registry.CompareValues(target.A, target.V.Span, src.GetValue(mid));
                    }
                    else
                    {
                        cmp = tCmp;
                    }

                }
                else
                {
                    cmp = entCmp;
                }
            }

            if (cmp > 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }
        return start;
    }

    private static int FindAVTEReader(this IReadable src, in Datom target, IAttributeRegistry registry)
    {
        var start = 0;
        var end = src.Length;

        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.A.CompareTo(src.GetAttributeId(mid));
            if (cmp == 0)
            {
                var valueCmp = registry.CompareValues(target.A, target.V.Span, src.GetValue(mid));
                if (valueCmp == 0)
                {
                    var tCmp = -target.T.CompareTo(src.GetTransactionId(mid));
                    if (tCmp == 0)
                    {
                        cmp = target.E.CompareTo(src.GetEntityId(mid));
                    }
                    else
                    {
                        cmp = tCmp;
                    }
                }
                else
                {
                    cmp = valueCmp;
                }
            }

            if (cmp > 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }
        return start;
    }

    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int FindEATV(this IReadable readable, in Datom target, IAttributeRegistry registry)
    {
        return readable.FindEATVReader(target, registry);
    }

    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int FindAETV(this IReadable readable, in Datom target, IAttributeRegistry registry)
    {
        return readable.FindAETVReader(target, registry);
    }

    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int FindAVTE(this IReadable readable, in Datom target, IAttributeRegistry registry)
    {
        return readable.FindAVTEReader(target, registry);
    }

    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int Find(this IReadable readable, in Datom target, SortOrders order, IAttributeRegistry registry)
    {
        return order switch
        {
            SortOrders.EATV => readable.FindEATV(target, registry),
            SortOrders.AETV => readable.FindAETV(target, registry),
            SortOrders.AVTE => readable.FindAVTE(target, registry),
            _ => throw new ArgumentOutOfRangeException(nameof(order), order, "Unknown sort order")
        };
    }

    /// <summary>
    /// Writes the node to the given <see cref="IBufferWriter{T}"/>.
    /// </summary>
    public static void WriteTo(this IReadable readable, IBufferWriter<byte> writer)
    {
        var packed = readable.Pack();

        if (packed is not DataPackedNode dataPackedNode)
        {
            throw new InvalidOperationException("The node is not a DataPackedNode.");
        }

        DataPackedNode.Serializer.Write(writer, dataPackedNode);
    }

    /// <summary>
    /// Packs the node into a new <see cref="IReadable"/>.
    /// </summary>
    public static IReadable Pack(this IReadable readable)
    {
        if (readable is IPacked packed)
        {
            return packed;
        }
        else if (readable is IAppendable appendable)
        {
            return appendable.Pack();
        }
        else
        {
            throw new InvalidOperationException("The node is neither packed nor appendable.");
        }
    }

    public static IReadable ReadDataNode(ReadOnlyMemory<byte> writerWrittenMemory)
    {
        var dataPackedNode = DataPackedNode.Serializer.Parse(writerWrittenMemory);
        return dataPackedNode;
    }
}
