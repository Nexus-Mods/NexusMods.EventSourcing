using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.FasterKV;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;
using NexusMods.Paths;
using Settings = NexusMods.EventSourcing.FasterKV.Settings;

namespace NexusMods.EventSourcing.Benchmarks;


[MemoryDiagnoser]
public class EntityContextBenchmarks : ABenchmark
{
    private EntityId<Loadout>[] _ids = Array.Empty<EntityId<Loadout>>();
    private EntityContext _context = null!;

    [Params(typeof(InMemoryEventStore<EventSerializer>),
        //typeof(FasterKVEventStore<EventSerializer>),
        typeof(RocksDBEventStore<EventSerializer>))]
    public Type EventStoreType { get; set; } = typeof(InMemoryEventStore<EventSerializer>);

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
        for (var e = 0; e < EntityCount; e++)
        {
            var evt = new CreateLoadout(EntityId<Loadout>.NewId(), $"Loadout {e}");
            EventStore.Add(evt);
            _ids[e] = evt.Id;
        }


        for (var ev = 0; ev < EventCount; ev++)
        {
            for (var e = 0; e < EntityCount; e++)
            {
                EventStore.Add(new RenameLoadout(_ids[e], $"Loadout {e} {ev}"));
            }
        }
    }

    [IterationSetup]
    public void Cleanup()
    {
        _context.EmptyCaches();
    }

    [Benchmark]
    public void LoadAllEntities()
    {
        var total = 0;
        var registry = _context.Get<LoadoutRegistry>();
        foreach (var loadout in registry.Loadouts)
        {
            total += loadout.Name.Length;
        }
    }

}
