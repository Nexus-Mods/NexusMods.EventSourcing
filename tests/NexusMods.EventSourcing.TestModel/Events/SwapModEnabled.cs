using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("8492075A-DED5-42BF-8D01-B4CDCE2526CF")]
public record SwapModEnabled(EntityId<Mod> ModId, bool Enabled) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
    }
}
