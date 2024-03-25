﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cathei.LinqGen;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage;

public static class RocksDBExtensions
{
    private static readonly ReadOptions DefaultReadOptions = new();

    public struct ValueRef : IDisposable
    {
        private IntPtr _ptr;
        private int _length;

        public ValueRef(IntPtr ptr, int length)
        {
            _ptr = ptr;
            _length = length;
        }

        /// <summary>
        /// True if the value is valid
        /// </summary>
        public bool IsValid => _ptr != IntPtr.Zero;

        public ReadOnlySpan<byte> Span
        {
            get
            {
                unsafe
                {
                    return new ReadOnlySpan<byte>(_ptr.ToPointer(), _length);
                }
            }
        }

        public void Dispose()
        {
            if (_ptr != IntPtr.Zero)
                Native.Instance.rocksdb_free(_ptr);
        }
    }

    public static ValueRef GetScoped<TKey>(this RocksDb db, ref TKey key, ColumnFamilyHandle columnFamily)
    where TKey : unmanaged
    {
        unsafe
        {
            var keySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
            fixed (byte *kPtr = keySpan)
            {
                var ptr = Native.Instance.rocksdb_get_cf(db.Handle, DefaultReadOptions.Handle, columnFamily.Handle, kPtr,
                    (UIntPtr)keySpan.Length, out var valLen, out var errPtr);
                if (ptr == IntPtr.Zero)
                {
                    return new ValueRef(IntPtr.Zero, 0);
                }
                if (errPtr != IntPtr.Zero)
                {
                    return new ValueRef(IntPtr.Zero, 0);
                }
                return new ValueRef(ptr, (int)valLen);
            }
        }
    }

    /// <summary>
    /// A wrapper around RocksDb iterator that uses structs and spans to avoid allocations. None of the spans from this
    /// iterator should be used outside of the scope of the iterator.
    /// </summary>
    public static ScopedIteratorThunk<TKey> GetScopedIterator<TKey>(this RocksDb db, TKey key, ColumnFamilyHandle columnFamily)
    where TKey : unmanaged
    {
        return new ScopedIteratorThunk<TKey>(key, db, columnFamily);
    }

    public readonly struct IteratorValue(Iterator iterator)
    {
        public ReadOnlySpan<byte> KeySpan => iterator.Key();

        public ReadOnlySpan<byte> ValueSpan => iterator.Value();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TKey Key<TKey>() where TKey : unmanaged
        {
            return MemoryMarshal.Cast<byte, TKey>(KeySpan)[0];
        }
    }

    public readonly struct ScopedIteratorThunk<TKey>(TKey key, RocksDb db, ColumnFamilyHandle handle)
        : IStructEnumerable<IteratorValue, ScopedIteratorEnumerator<TKey>>
        where TKey : unmanaged
    {
        public ScopedIteratorEnumerator<TKey> GetEnumerator()
        {
            return new ScopedIteratorEnumerator<TKey>(key, db.NewIterator(handle), false);
        }
    }

    public struct ScopedIteratorEnumerator<TKey>(TKey key, Iterator iterator, bool primed)
        : IEnumerator<IteratorValue>
        where TKey : unmanaged
    {
        public bool MoveNext()
        {
            if (!primed)
            {
                var keySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
                iterator.Seek(keySpan);
                primed = true;

                return iterator.Valid();
            }

            iterator.Next();
            return iterator.Valid();
        }

        public void Reset() => throw new NotSupportedException();

        public IteratorValue Current => new(iterator);

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            iterator.Dispose();
        }
    }
}
