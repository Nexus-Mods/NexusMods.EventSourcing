﻿using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Datoms;

public unsafe struct MemoryDatom<T>
where T : IBlobColumn
{
    public EntityId* EntityIds;
    public AttributeId* AttributeIds;
    public TxId* TransactionIds;
    public DatomFlags* Flags;
    public T Values;
}
