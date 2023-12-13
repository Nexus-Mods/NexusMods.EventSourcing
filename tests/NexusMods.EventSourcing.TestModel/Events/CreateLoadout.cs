using DynamicData;
using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("63A4CB90-27E2-468A-BE94-CB01A38D8C09")]
[MemoryPackable]
public partial record CreateLoadout(EntityId<Loadout> Id, string Name) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
        context.New(Id);
        context.Emit(Id, Loadout._name, Name);
        context.Emit(LoadoutRegistry.SingletonId, LoadoutRegistry._loadouts, Id);
    }
    public static CreateLoadout Create(string name) => new(EntityId<Loadout>.NewId(), name);
}
