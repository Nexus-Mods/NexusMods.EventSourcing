﻿using System;
using System.Collections.Generic;
using Cathei.LinqGen;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage;

public class Index<TComparator> where TComparator : IDatomComparator
{
    private INode _topBlock;
    private readonly TComparator _comparator;

    public Index(TComparator comparator, AttributeRegistry registry, SortType indexType)
    {
        _comparator = comparator;
        throw new NotImplementedException();
        //_topBlock = new OldAppendableNode(configuration);
    }

    public void Ingest<TIterator, TDatom>(in TIterator other)
        where TIterator : IIterator<TDatom>
        where TDatom : IRawDatom
    {
        _topBlock = _topBlock.Ingest<TIterator, TDatom, OnHeapDatom, TComparator>(in other, OnHeapDatom.Max, _comparator);
        if (_topBlock.ChildCount > Configuration.IndexBlockSize * 2)
        {
            _topBlock = new IndexNode(_topBlock);
        }
    }

    /// <summary>
    /// Writes the current index to the KV store.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void Commit<TStore>(TStore kvStore) where TStore : IKvStore
    {
        throw new NotImplementedException();
    }

    public int Count => _topBlock.Count;

    public int ChildCount => _topBlock.ChildCount;

    /// <summary>
    /// Rather slow, but returns the nth datom in the index.
    /// </summary>
    /// <param name="idx"></param>
    public IRawDatom this[int idx] => _topBlock[idx];

    public void Flush(NodeStore nodeStore)
    {
        _topBlock = _topBlock.Flush(nodeStore);
    }
}
