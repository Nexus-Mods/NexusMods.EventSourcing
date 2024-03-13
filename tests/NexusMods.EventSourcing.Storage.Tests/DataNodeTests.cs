﻿using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Tests;

public class DataNodeTests(IServiceProvider provider) : AStorageTest(provider)
{




    [Theory]
    [InlineData(SortOrders.EATV)]
    [InlineData(SortOrders.AETV)]
    [InlineData(SortOrders.AVTE)]
    public void CanMergeBlock(SortOrders orders)
    {
        var block = new Appendable();
        var allDatoms = TestData(10).ToArray();

        Random.Shared.Shuffle(allDatoms);

        var block2 = new Appendable();

        var half = allDatoms.Length / 2;
        for (var i = 0; i < half; i++)
        {
            block.Add(in allDatoms[i]);
        }

        for (var i = half; i < allDatoms.Length; i++)
        {
            block2.Add(in allDatoms[i]);
        }

        var compare = _registry.CreateComparator(orders);

        block.Sort(compare);
        block2.Sort(compare);

        throw new NotImplementedException();
        /*
        var joined = SortedMerge.Merge(block, block2, compare);

        var sorted = allDatoms.Order(CreateComparer(compare))
            .ToArray();

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = joined[i];
            var datomB = sorted[i];
            AssertEqual(datomA, datomB, i);
        }
        */
    }

    [Theory]
    [InlineData(SortOrders.EATV)]
    [InlineData(SortOrders.AETV)]
    [InlineData(SortOrders.AVTE)]
    public void CanSeekToDatom(SortOrders order)
    {
        var compare = _registry.CreateComparator(order);
        var block = new Appendable();
        var allDatoms = TestData(10).ToArray();
        foreach (var datom in allDatoms)
        {
            block.Add(in datom);
        }

        var sorted = allDatoms.Order(CreateComparer(compare)).ToArray();
        block.Sort(compare);

        throw new NotImplementedException();
        /*
        for (var i = 0; i < sorted.Length; i++)
        {
            var datom = sorted[i];
            var idx = block.Find(0, block.Length, in datom, order, _registry);

            idx.Should().Be(i, "both sources are sorted and should match");

            var found = block[idx];
            AssertEqual(found, datom, i);
        }
        */
    }


    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(128)]
    [InlineData(1024)]
    [InlineData(1024 * 8)]
    public void CanReadAndWriteBlocks(uint count)
    {
        var allDatoms = TestData(count).ToArray();
        var block = new Appendable();
        foreach (var datom in allDatoms)
        {
            block.Add(in datom);
        }

        var packed = block.Pack();

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = packed[i];
            var datomB = allDatoms[i];

            AssertEqual(datomA, datomB, i);
        }

        /*
        var writer = new PooledMemoryBufferWriter();
        packed.WriteTo(writer);


        Logger.LogInformation("Packed {0} datoms into {1} bytes", packed.Length, writer.WrittenMemory.Length);
        Logger.LogInformation("Average size: {0} bytes", writer.WrittenMemory.Length / packed.Length);
        var uncompressedSize = packed.Length * (8 + 8 + 1 + 8);
        Logger.LogInformation("Compression ratio: {0}%", (writer.WrittenMemory.Length * 100) / uncompressedSize);

        var read = NodeReader.ReadDataNode(writer.WrittenMemory);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = read[i];
            var datomB = allDatoms[i];

            AssertEqual(datomA, datomB, i);
        }
        */
        throw new NotImplementedException();
    }



    public IComparer<Datom> CreateComparer(IDatomComparator datomComparator)
    {
        return Comparer<Datom>.Create((a, b) => datomComparator.Compare(in a, in b));
    }

    private IEnumerable<Datom> TestData(uint max)
    {
        for (ulong eid = 0; eid < max; eid += 1)
        {
            for (ulong tx = 0; tx < 10; tx += 1)
            {
                for (ulong val = 1; val < 10; val += 1)
                {
                    yield return new Datom()
                    {
                        E = EntityId.From(eid),
                        A = AttributeId.From(10),
                        T = TxId.From(tx),
                        V = BitConverter.GetBytes(val)
                    };
                }
            }
        }
    }

}
