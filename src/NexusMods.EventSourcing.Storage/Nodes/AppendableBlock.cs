﻿using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cathei.LinqGen;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class AppendableBlock :
    INode,
    IStructEnumerable<AppendableBlock.FlyweightDatom, AppendableBlock.FlyweightDatomEnumerator>
{
    private readonly PooledMemoryBufferWriter _pooledMemoryBufferWriter = new();
    private readonly List<ulong> _entityIds = new();
    private readonly List<ushort> _attributeIds = new();
    private readonly List<ulong> _txIds = new();
    private readonly List<byte> _flags = new();
    private readonly List<ulong> _values = new();
    public int Count => _entityIds.Count;

    public void Append<TRawDatom>(in TRawDatom datom)
    where TRawDatom : IRawDatom
    {
        _entityIds.Add(datom.EntityId);
        _attributeIds.Add(datom.AttributeId);
        _txIds.Add(datom.TxId);
        _flags.Add(datom.Flags);
        _values.Add(datom.ValueLiteral);
        if (((DatomFlags)datom.Flags).HasFlag(DatomFlags.InlinedData))
            return;

        var span = datom.ValueSpan;
        var offset = _pooledMemoryBufferWriter.GetWrittenSpan().Length;
        _values.Add((ulong)((offset << 4) | span.Length));
        _pooledMemoryBufferWriter.Write(span);
    }

    public void Sort<TComparer>(TComparer comparer)
        where TComparer : IDatomComparator<FlyweightDatom, FlyweightDatom>
    {
        var indexes = GC.AllocateUninitializedArray<int>(_entityIds.Count);
        for (var i = 0; i < indexes.Length; i++)
        {
            indexes[i] = i;
        }

        Array.Sort(indexes, new OuterComparator<TComparer>(this, comparer));

        for (var i = 0; i < indexes.Length; i++)
        {
            var j = indexes[i];
            (_entityIds[i], _entityIds[j]) = (_entityIds[j], _entityIds[i]);
            (_attributeIds[i], _attributeIds[j]) = (_attributeIds[j], _attributeIds[i]);
            (_txIds[i], _txIds[j]) = (_txIds[j], _txIds[i]);
            (_flags[i], _flags[j]) = (_flags[j], _flags[i]);
            (_values[i], _values[j]) = (_values[j], _values[i]);
        }
    }

    public void WriteTo<TBufferWriter>(TBufferWriter writer)
        where TBufferWriter : IBufferWriter<byte>
    {
        unsafe
        {
            var headerSpan = writer.GetSpan(sizeof(BlockHeader));
            ref var header = ref MemoryMarshal.AsRef<BlockHeader>(headerSpan);
            header._datomCount = (uint)_entityIds.Count;
            header._blobSize = (uint)_pooledMemoryBufferWriter.GetWrittenSpan().Length;
            header._version = 0x01;
            header._flags = 0x00;
            writer.Advance(sizeof(BlockHeader));

            var count = (int)header._datomCount;
            var span = writer.GetSpan(_entityIds.Count * sizeof(ulong));
            MemoryMarshal.Cast<ulong, byte>(CollectionsMarshal.AsSpan(_entityIds)).SliceFast(0, count).CopyTo(span);
            writer.Advance(_entityIds.Count * sizeof(ulong));

            span = writer.GetSpan(_attributeIds.Count * sizeof(ushort));
            MemoryMarshal.Cast<ushort, byte>(CollectionsMarshal.AsSpan(_attributeIds)).SliceFast(0, count).CopyTo(span);
            writer.Advance(_attributeIds.Count * sizeof(ushort));

            span = writer.GetSpan(_txIds.Count * sizeof(ulong));
            MemoryMarshal.Cast<ulong, byte>(CollectionsMarshal.AsSpan(_txIds)).SliceFast(0, count).CopyTo(span);
            writer.Advance(_txIds.Count * sizeof(ulong));

            span = writer.GetSpan(_flags.Count * sizeof(byte));
            CollectionsMarshal.AsSpan(_flags).SliceFast(0, count).CopyTo(span);
            writer.Advance(_flags.Count * sizeof(byte));

            span = writer.GetSpan(_values.Count * sizeof(ulong));
            MemoryMarshal.Cast<ulong, byte>(CollectionsMarshal.AsSpan(_values)).SliceFast(0, count).CopyTo(span);
            writer.Advance(_values.Count * sizeof(ulong));

            var pooledWrittenSpan = _pooledMemoryBufferWriter.GetWrittenSpan();
            span = writer.GetSpan(pooledWrittenSpan.Length);
            pooledWrittenSpan.SliceFast(0, pooledWrittenSpan.Length).CopyTo(span);
            writer.Advance(pooledWrittenSpan.Length);
        }
    }

    /// <summary>
    /// Initializes the block from a span of bytes, the bytes can be written by the WriteTo method.
    /// </summary>
    /// <param name="span"></param>
    public void InitializeFrom(ReadOnlySpan<byte> span)
    {
        // Casts and copies the data from the span into the list
        void CopyToList<TValue>(List<TValue> list, ReadOnlySpan<byte> fromSpan, int count)
            where TValue : struct
        {
            var listSpan = MemoryMarshal.Cast<byte, TValue>(fromSpan).SliceFast(0, count);
            list.Clear();
            list.AddRange(listSpan);
        }

        unsafe
        {
            var header = MemoryMarshal.Read<BlockHeader>(span);
            var dataSection = span.SliceFast(sizeof(BlockHeader));

            CopyToList(_entityIds, dataSection, (int)header._datomCount);
            dataSection = dataSection.SliceFast((int)header._datomCount * sizeof(ulong));

            CopyToList(_attributeIds, dataSection, (int)header._datomCount);
            dataSection = dataSection.SliceFast((int)header._datomCount * sizeof(ushort));

            CopyToList(_txIds, dataSection, (int)header._datomCount);
            dataSection = dataSection.SliceFast((int)header._datomCount * sizeof(ulong));

            CopyToList(_flags, dataSection, (int)header._datomCount);
            dataSection = dataSection.SliceFast((int)header._datomCount * sizeof(byte));

            CopyToList(_values, dataSection, (int)header._datomCount);
            dataSection = dataSection.SliceFast((int)header._datomCount * sizeof(ulong));

            _pooledMemoryBufferWriter.Write(dataSection);
        }
    }

    public int BinarySearch<TDatomIn>(ref TDatomIn datom, IDatomComparator<TDatomIn, FlyweightDatom> comparator)
        where TDatomIn : IRawDatom
    {
        int lower = 0;
        int upper = Count - 1;

        while (lower <= upper)
        {
            int middle = lower + ((upper - lower) / 2);
            var middleDatom = new FlyweightDatom(this, (uint)middle);

            int comparison = comparator.Compare(datom, middleDatom);
            if (comparison == 0)
            {
                return middle;
            }
            else if (comparison < 0)
            {
                upper = middle - 1;
            }
            else
            {
                lower = middle + 1;
            }
        }

        return -1; // datom not found
    }

    public FlyweightDatomEnumerator Seek<TDatomIn>(TDatomIn datom, IDatomComparator<TDatomIn, FlyweightDatom> comparator)
        where TDatomIn : IRawDatom
    {
        var index = BinarySearch(ref datom, comparator);
        return new FlyweightDatomEnumerator(this, index);
    }

    private class OuterComparator<TComparer>(AppendableBlock block, TComparer comparer) : IComparer<int>
        where TComparer : IDatomComparator<FlyweightDatom, FlyweightDatom>
    {
        public int Compare(int x, int y)
        {
            var a = new FlyweightDatom(block, (uint)x);
            var b = new FlyweightDatom(block, (uint)y);
            return comparer.Compare(a, b);
        }
    }



    public FlyweightDatom this[int index] => new(this, (uint)index);


    public struct FlyweightDatom(AppendableBlock block, uint index) : IRawDatom
    {
        public ulong EntityId => block._entityIds[(int)index];
        public ushort AttributeId => block._attributeIds[(int)index];
        public ulong TxId => block._txIds[(int)index];
        public byte Flags => block._flags[(int)index];
        public ReadOnlySpan<byte> ValueSpan
        {
            get
            {
                if (((DatomFlags)Flags).HasFlag(DatomFlags.InlinedData))
                {
                    return ReadOnlySpan<byte>.Empty;
                }
                var combined = block._values[(int)index];
                var offset = (uint)(combined >> 4);
                var length = (uint)(combined & 0xF);
                return block._pooledMemoryBufferWriter.GetWrittenSpan().Slice((int)offset, (int)length);
            }
        }
        public ulong ValueLiteral => block._values[(int)index];

        public void Expand<TWriter>(out ulong entityId, out ushort attributeId, out ulong txId, out byte flags, in TWriter writer,
            out ulong valueLiteral) where TWriter : IBufferWriter<byte>
        {
            throw new NotImplementedException();
        }
    }

    public struct FlyweightDatomEnumerator(AppendableBlock block, int idx)
        : IEnumerator<FlyweightDatom>
    {
        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (idx >= block._entityIds.Count) return false;
            idx++;
            return true;
        }

        public void Reset()
        {
            idx = -1;
        }

        public FlyweightDatom Current => new(block, (uint)idx);

        object IEnumerator.Current => Current;
    }

    public FlyweightDatomEnumerator GetEnumerator()
    {
        return new(this, -1);
    }

    #region INode



    public INode Insert<TInput>(TInput inputDatom)
    {
        throw new NotImplementedException();
    }

    public INode Remove<TInput>(TInput inputDatom)
    {
       var index = BinarySearch(ref inputDatom, new DatomComparator());
    }

    public INode Merge(INode other)
    {
        var newNode = new AppendableBlock();
        foreach (var datom in this)
        {
            newNode.Append(datom);
        }

        foreach (var datom in other)
        {
            newNode.Append(datom);
        }

        return newNode;

    }

    public (INode, INode) Split()
    {
        var splitPoint = Count / 2;
        var left = new AppendableBlock();
        var right = new AppendableBlock();

        for (var i = 0; i < splitPoint; i++)
        {
            left.Append(this[i]);
        }

        for (var i = splitPoint; i < Count; i++)
        {
            right.Append(this[i]);
        }

        return (left, right);
    }
    #endregion


}
