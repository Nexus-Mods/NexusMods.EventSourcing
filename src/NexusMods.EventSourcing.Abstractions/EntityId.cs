﻿using System.Numerics;
using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A unique identifier for an entity.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct EntityId
{
    public static EntityId MinValue => new(Ids.MakeId(Ids.Partition.Entity, 1));
}

