using DynamicData;
using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("7DC8F80B-50B6-43B7-B805-43450E9F0C2B")]
[MemoryPackable]
public partial record AddMod(string Name, bool Enabled, EntityId<Mod> ModId, EntityId<Loadout> LoadoutId) : IEvent
{
    public async ValueTask Apply<T>(T context) where T : IEventContext
    {
        context.New(ModId);
        context.Emit(ModId, Mod._name, Name);
        context.Emit(ModId, Mod._enabled, Enabled);
        context.Emit(ModId, Mod._loadout, LoadoutId);
        context.Emit(LoadoutId, Loadout._mods, ModId);
    }

    /// <summary>
    /// Creates a event that adds a new mod to the given loadout giving it the given name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="loadoutId"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public static AddMod Create(string name, EntityId<Loadout> loadoutId, bool enabled = true)
        => new(name, enabled, EntityId<Mod>.NewId(), loadoutId);
}
