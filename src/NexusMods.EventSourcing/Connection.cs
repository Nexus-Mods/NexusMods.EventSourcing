﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.DatomStore;

namespace NexusMods.EventSourcing;

/// <summary>
/// Main connection class, co-ordinates writes and immutable reads
/// </summary>
public class Connection : IConnection
{
    private readonly object _lock = new();
    private ulong _nextEntityId = Ids.MinId(Ids.Partition.Entity);
    private readonly IDatomStore _store;
    private readonly IAttribute[] _declaredAttributes;
    internal readonly ModelReflector<Transaction> ModelReflector;
    private readonly Subject<ICommitResult> _updates;

    /// <summary>
    /// Main connection class, co-ordinates writes and immutable reads
    /// </summary>
    public Connection(IDatomStore store, IEnumerable<IAttribute> declaredAttributes, IEnumerable<IValueSerializer> serializers)
    {
        _store = store;
        _declaredAttributes = declaredAttributes.ToArray();
        ModelReflector = new ModelReflector<Transaction>(store);

        _updates = new Subject<ICommitResult>();

        AddMissingAttributes(serializers);
    }

    private void AddMissingAttributes(IEnumerable<IValueSerializer> valueSerializers)
    {
        var serializerByType = valueSerializers.ToDictionary(s => s.NativeType);

        var existing = ExistingAttributes().ToDictionary(a => a.UniqueId);

        var missing = _declaredAttributes.Where(a => !existing.ContainsKey(a.Id)).ToArray();
        if (missing.Length == 0)
            return;

        var datoms = new List<IDatom>();

        var newAttrs = new List<DbAttribute>();

        var attrId = existing.Values.Max(a => a.AttrEntityId);
        foreach (var attr in missing)
        {
            var id = ++attrId;

            var serializer = serializerByType[attr.ValueType];
            var uniqueId = attr.Id;
            datoms.Add(new AssertDatom<BuiltInAttributes.UniqueId, Symbol>(id, uniqueId));
            datoms.Add(new AssertDatom<BuiltInAttributes.ValueSerializerId, UInt128>(id, serializer.UniqueId));
            newAttrs.Add(new DbAttribute(uniqueId, id, serializer.UniqueId));
        }
        TxId = _store.Transact(datoms);

        _store.RegisterAttributes(newAttrs);

    }

    private IEnumerable<DbAttribute> ExistingAttributes()
    {
        var tx = TxId.MaxValue;
        var attrIterator = _store.Where<BuiltInAttributes.UniqueId>(tx);
        var entIterator = _store.EntityIterator(tx);
        while (attrIterator.Next())
        {
            entIterator.Set(attrIterator.EntityId);

            var serializerId = UInt128.Zero;
            Symbol uniqueId = null!;

            while (entIterator.Next())
            {
                var current = entIterator.Current;
                switch (current)
                {
                    case AssertDatom<BuiltInAttributes.ValueSerializerId, UInt128> serializerIdDatom:
                        serializerId = serializerIdDatom.V;
                        break;
                    case AssertDatom<BuiltInAttributes.UniqueId, Symbol> uniqueIdDatom:
                        uniqueId = uniqueIdDatom.V;
                        break;
                }
            }
            yield return new DbAttribute(uniqueId, attrIterator.EntityId.Value, serializerId);
        }
    }


    /// <inheritdoc />
    public IDb Db => new Db(_store, this, TxId);


    /// <inheritdoc />
    public TxId TxId { get; private set; }


    /// <inheritdoc />
    public ICommitResult Transact(IEnumerable<IDatom> datoms)
    {
        var remaps = new Dictionary<ulong, ulong>();
        var datomsArray = datoms.ToArray();

        EntityId RemapFn(EntityId input)
        {
            if (Ids.GetPartition(input) == Ids.Partition.Tmp)
            {
                if (!remaps.TryGetValue(input.Value, out var id))
                {
                    var newId = _nextEntityId++;
                    remaps[input.Value] = newId;
                    return EntityId.From(newId);
                }
                return EntityId.From(id);
            }
            return input;
        }

        lock (_lock)
        {
            var newDatoms = new List<IDatom>();
            foreach (var datom in datomsArray)
            {
                datom.Remap(RemapFn);
                newDatoms.Add(datom);
            }
            var newTx = _store.Transact(newDatoms);
            TxId = newTx;
            var result = new CommitResult(newTx, remaps, datomsArray);
            _updates.OnNext(result);
            return result;
        }
    }

    /// <inheritdoc />
    public ITransaction BeginTransaction()
    {
        return new Transaction(this);
    }

    /// <inheritdoc />
    public IObservable<ICommitResult> Commits => _updates;
}
