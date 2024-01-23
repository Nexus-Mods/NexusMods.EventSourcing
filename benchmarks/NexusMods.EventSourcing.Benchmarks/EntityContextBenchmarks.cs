using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Benchmarks;


[MemoryDiagnoser]
public class EntityContextBenchmarks : ABenchmark
{
    private EntityId<Loadout>[] _ids = Array.Empty<EntityId<Loadout>>();
    private EntityContext _context = null!;

    [Params(//typeof(InMemoryEventStore<BinaryEventSerializer>),
        //typeof(FasterKVEventStore<BinaryEventSerializer>),
        typeof(RocksDBEventStore<BinaryEventSerializer>))]
    public Type EventStoreType { get; set; } = typeof(InMemoryEventStore<BinaryEventSerializer>);

    [Params(100, 1000)]
    public int EventCount { get; set; }

    [Params(100, 1000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        MakeStore(EventStoreType);
        _context = new EntityContext(EventStore);

        _ids = new EntityId<Loadout>[EntityCount];

        var lsts = new List<EntityId<Loadout>>();

        for (var e = 0; e < EntityCount; e++)
        {
            using var tx = _context.Begin();
            var loadout = CreateLoadout.Create(tx, $"Loadout {e}");
            tx.Commit();
        }


        for (var ev = 0; ev < EventCount; ev++)
        {
            for (var e = 0; e < EntityCount; e++)
            {
                using var tx = _context.Begin();
                RenameLoadout.Create(tx, _ids[e], $"Loadout {e} {ev}");
                tx.Commit();
            }
        }
    }

    [Benchmark]
    public void LoadAllEntities()
    {
        _context.EmptyCaches();
        var total = 0;
        var registry = _context.Get<LoadoutRegistry>();
        foreach (var loadout in registry.Loadouts)
        {
            total += loadout.Name.Length;
        }
    }

}
