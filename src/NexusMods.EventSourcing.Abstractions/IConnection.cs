﻿using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A mutable connection to a data source
/// </summary>
public interface IConnection
{
    /// <summary>
    /// Starts a new transaction
    /// </summary>
    /// <returns></returns>
    public Transaction BeginTransaction()
    {
        return new Transaction(this);
    }

    /// <summary>
    /// Commits the changes in the transaction to the data source
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    TransactionId Commit(IDictionary<EntityId, AEntity> attachedEntities, IReadOnlyCollection<(ulong E, ulong A, object v)> changes);
}
