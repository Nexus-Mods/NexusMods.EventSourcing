using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

/// <summary>
/// An event context that forwards events to a dictionary of accumulators, and is tuned for adding new values to existing
/// accumulators and entities. In other words is for moving a
/// </summary>
public readonly struct ForwardEventContext(ConcurrentDictionary<EntityId, Dictionary<IAttribute, IAccumulator>> trackedEntities, HashSet<(EntityId, string)> updatedAttributes) : IEventContext
{
    /// <inheritdoc />
    public bool GetAccumulator<TOwner, TAttribute, TAccumulator>(EntityId<TOwner> entityId, TAttribute attributeDefinition, out TAccumulator accumulator)
        where TOwner : IEntity
        where TAttribute : IAttribute<TAccumulator>
        where TAccumulator : IAccumulator

    {
        updatedAttributes.Add((entityId.Value, attributeDefinition.Name));

        if (!trackedEntities.TryGetValue(entityId.Value, out var values))
        {
            accumulator = default!;
            return false;
        }

        if (values.TryGetValue(attributeDefinition, out var found))
        {
            accumulator = (TAccumulator)found;
            return true;
        }

        var newAccumulator = attributeDefinition.CreateAccumulator();
        values.Add(attributeDefinition, newAccumulator);
        accumulator = newAccumulator;
        return true;
    }
}
