﻿using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Tests;

namespace NexusMods.EventSourcing.Storage.Benchmarks;

public class IteratorBenchmarks : AStorageBenchmark
{
    private IDatomComparator _sorter = null!;
    private AppendableIndexNode _index = null!;

    [Params(2, 128)]
    public ulong Count { get; set; }

    [Params(2, 128)]
    public ulong TxCount { get; set; }

    [Params(SortOrders.EATV, SortOrders.AVTE, SortOrders.AETV)]
    public SortOrders SortOrder { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _sorter = _registry.CreateComparator(SortOrder);
        _index = new AppendableIndexNode(_sorter);

        var emitters = new Action<AppendableNode, EntityId, TxId, ulong>[]
        {
            (chunk, e, tx, v) => _registry.Append<TestAttributes.FileHash, ulong>(chunk, e, tx, DatomFlags.Added, v),
            (chunk, e, tx, v) => _registry.Append<TestAttributes.FileName, string>(chunk, e, tx, DatomFlags.Added, "file " + v),
        };

        for (ulong tx = 0; tx < TxCount; tx++)
        {
            var chunk = new AppendableNode();
            for (ulong e = 0; e < Count; e++)
            {
                for (var a = 0; a < 2; a++)
                {
                    emitters[a](chunk, EntityId.From(e), TxId.From(tx), tx);
                }
            }
            chunk.Sort(_sorter);
            _index = _index.Ingest(chunk);
        }
    }

    [Benchmark]
    public ulong Lookup()
    {
        return _index[_index.Length - 1].E.Value;
    }
}
