﻿using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
///     Represents a connection to a database.
/// </summary>
public interface IConnection
{
    /// <summary>
    ///     Gets the current database.
    /// </summary>
    public IDb Db { get; }

    /// <summary>
    ///     Gets the most recent transaction id.
    /// </summary>
    public TxId TxId { get; }

    /// <summary>
    ///     A sequential stream of database revisions.
    /// </summary>
    public IObservable<IDb> Revisions { get; }

    /// <summary>
    ///     Starts a new transaction.
    /// </summary>
    /// <returns></returns>
    public ITransaction BeginTransaction();
}
