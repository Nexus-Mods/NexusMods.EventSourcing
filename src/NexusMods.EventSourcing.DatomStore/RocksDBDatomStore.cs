﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.DatomStore.Indexes;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.DatomStore;

public class RocksDBDatomStore : IDatomStore
{
    private object _lock = new();
    private readonly ILogger<RocksDBDatomStore> _logger;
    private readonly DatomStoreSettings _settings;
    private readonly DbOptions _options;
    private readonly RocksDb _db;
    private readonly PooledMemoryBufferWriter _pooledWriter;
    private readonly AttributeRegistry _registry;
    private readonly AIndexDefinition[] _indexes;
    private ulong _tx;
    private readonly AETVIndex _avetIndex;
    private readonly EATVIndex _eatvIndex;

    public RocksDBDatomStore(ILogger<RocksDBDatomStore> logger, AttributeRegistry registry, DatomStoreSettings settings)
    {
        _settings = settings;
        _logger = logger;

        _registry = registry;
        _registry.Populate(BuiltInAttributes.Initial);

        _options = new DbOptions()
            .SetCreateIfMissing()
            .SetCompression(Compression.Zstd);

        _db = RocksDb.Open(_options, _settings.Path.ToString(), new ColumnFamilies());

        _eatvIndex = new EATVIndex(_registry);
        _avetIndex = new AETVIndex(_registry);
        _indexes =
        [
            //new TxIndex(_registry),
            _eatvIndex,
            _avetIndex,
        ];

        foreach (var index in _indexes)
        {
            index.Init(_db);
        }

        _pooledWriter = new PooledMemoryBufferWriter(128);

        _tx = 0;

        Transact(BuiltInAttributes.InitialDatoms);
    }


    private void Serialize<TAttr, TVal>(WriteBatch batch, ulong e, TVal val, ulong tx, bool isAssert = true)
        where TAttr : IAttribute<TVal>

    {
        _pooledWriter.Reset();
        var header = _pooledWriter.GetSpan(KeyHeader.Size);
        ref var keyHeader = ref MemoryMarshal.AsRef<KeyHeader>(header);
        keyHeader.Entity = e;
        keyHeader.AttributeId = _registry.GetAttributeId<TAttr>();
        keyHeader.IsAssert = isAssert;
        keyHeader.Tx = tx;
        _pooledWriter.Advance(KeyHeader.Size);
        _registry.WriteValue(val, in _pooledWriter);

        var span = _pooledWriter.GetWrittenSpan();
        foreach (var index in _indexes)
        {
            index.Put(batch, span);
        }
    }

    private struct TransactSink(RocksDBDatomStore store, WriteBatch batch, ulong tx) : IDatomSink
    {
        public void Datom<TAttr, TVal>(ulong e, TVal v, bool isAssert) where TAttr : IAttribute<TVal>
        {
            store.Serialize<TAttr, TVal>(batch, e, v,tx, isAssert);
        }
    }

    /// <summary>
    /// Returns the highest entity id in the given partition.
    /// </summary>
    /// <param name="partition"></param>
    /// <returns></returns>
    public ulong MaxId(Ids.Partition partition)
    {
        if (_eatvIndex.MaxId(partition, out var maxId))
        {
            return maxId;
        }
        return Ids.MaxId(partition);
    }

    public TxId Transact(IEnumerable<IDatom> datoms)
    {
        lock (_lock)
        {
            var tx = ++_tx;
            var batch = new WriteBatch();
            var sink = new TransactSink(this, batch, tx);

            foreach (var datom in datoms)
            {
                datom.Emit(ref sink);
            }

            _db.Write(batch);
            return TxId.From(tx);
        }
    }

    public IIterator Where<TAttr>(TxId txId) where TAttr : IAttribute
    {
        return new AETVIndex.AEVTByAIterator<TAttr>(txId.Value, _registry, _avetIndex);
    }

    public IEntityIterator EntityIterator(TxId txId)
    {
        return new EATVIndex.EATVIterator(txId.Value, _registry, _eatvIndex);
    }

    public void RegisterAttributes(IEnumerable<DbAttribute> newAttrs)
    {
        _registry.Populate(newAttrs.ToArray());
    }

    public void Dispose()
    {
        _db.Dispose();
        _avetIndex.Dispose();
        _eatvIndex.Dispose();
    }
}
