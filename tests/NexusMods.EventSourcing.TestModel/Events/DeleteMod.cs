using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("5CD171BF-4FFE-40E5-819B-987C48A20DF6")]
[MemoryPackable]
public partial record DeleteMod(EntityId<Mod> ModId, EntityId<Loadout> LoadoutId) : IEvent
{
    public ValueTask Apply<T>(T context) where T : IEventContext
    {
        context.Retract(LoadoutId, Loadout._mods, ModId);
        context.Retract(ModId, Mod._loadout, LoadoutId);
        return ValueTask.CompletedTask;
    }
}
