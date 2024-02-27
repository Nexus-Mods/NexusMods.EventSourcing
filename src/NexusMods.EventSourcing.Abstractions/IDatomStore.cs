﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents the low-level storage for datoms.
/// </summary>
public interface IDatomStore : IDisposable
{

    /// <summary>
    /// Transacts (adds) the given datoms into the store.
    /// </summary>
    public TxId Transact(IEnumerable<ITypedDatom> datoms);

    /// <summary>
    /// Returns all the most recent datoms (less than or equal to txId) with the given attribute.
    /// </summary>
    /// <param name="txId"></param>
    /// <typeparam name="TAttr"></typeparam>
    /// <returns></returns>
    //IIterator Where<TAttr>(TxId txId) where TAttr : IAttribute;

    /// <summary>
    /// Creates an iterator over all entities.
    /// </summary>
    /// <param name="txId"></param>
    /// <returns></returns>
    IEntityIterator EntityIterator(TxId txId);

    /// <summary>
    /// Registers new attributes with the store. These should already have been transacted into the store.
    /// </summary>
    /// <param name="newAttrs"></param>
    void RegisterAttributes(IEnumerable<DbAttribute> newAttrs);

    /// <summary>
    /// Gets the attributeId for the given attribute. And returns an expression that reads the attribute
    /// value from the expression valueSpan.
    /// </summary>
    Expression GetValueReadExpression(Type attribute, Expression valueSpan, out ulong attributeId);

    /// <summary>
    /// Gets all the entities that reference the given entity id with the given attribute.
    /// </summary>
    IEnumerable<EntityId> ReverseLookup<TAttribute>(TxId txId) where TAttribute : IAttribute<EntityId>;
}
