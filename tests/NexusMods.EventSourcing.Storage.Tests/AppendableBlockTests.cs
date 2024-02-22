﻿using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Sorters;

namespace NexusMods.EventSourcing.Storage.Tests;

public class AppendableBlockTests
{

    [Fact]
    public void CanAppendDataToBlock()
    {
        var block = new AppendableBlock();
        var allDatoms = TestData(10).ToArray();
        foreach (var datom in TestData(10))
        {
            block.Append(in datom);
        }

        block.Count.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = allDatoms[i];

            AssertEqual(datomA, datomB, i);
        }
    }

    [Fact]
    public void CanSortBlock()
    {
        var block = new AppendableBlock();
        var allDatoms = TestData(10).ToArray();
        Random.Shared.Shuffle(allDatoms);

        foreach (var datom in TestData(10))
        {
            block.Append(in datom);
        }

        block.Sort(new Eatv<AppendableBlock.FlyweightDatom, AppendableBlock.FlyweightDatom>());

        var sorted = allDatoms.OrderBy(d => d.EntityId)
            .ThenBy(d => d.AttributeId)
            .ThenBy(d => d.TxId)
            .ThenBy(d => d.ValueLiteral)
            .ToArray();

        block.Count.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = sorted[i];

            AssertEqual(datomA, datomB, i);
        }
    }

    [Fact]
    public void CanReadAndWriteBlocks()
    {
        var allDatoms = TestData(10).ToArray();
        var block = new AppendableBlock();
        foreach (var datom in TestData(10))
        {
            block.Append(in datom);
        }

        var writer = new PooledMemoryBufferWriter();
        block.WriteTo(writer);

        var block2 = new AppendableBlock();
        block2.InitializeFrom(writer.GetWrittenSpan());

        block2.Count.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = allDatoms[i];

            AssertEqual(datomA, datomB, i);
        }
    }

    private static void AssertEqual(in AppendableBlock.FlyweightDatom datomA, IRawDatom datomB, int i)
    {
        datomA.EntityId.Should().Be(datomB.EntityId, "at index " + i);
        datomA.AttributeId.Should().Be(datomB.AttributeId, "at index " + i);
        datomA.TxId.Should().Be(datomB.TxId, "at index " + i);
        datomA.Flags.Should().Be(datomB.Flags, "at index " + i);
        datomA.ValueLiteral.Should().Be(datomB.ValueLiteral, "at index " + i);
        datomA.ValueSpan.SequenceEqual(datomB.ValueSpan).Should().BeTrue("at index " + i);
    }


    public IEnumerable<IRawDatom> TestData(uint max)
    {
        for (ulong eid = 0; eid < max; eid += 1)
        {
            for (ushort aid = 0; aid < 10; aid += 1)
            {
                for (ulong tx = 0; tx < 10; tx += 1)
                {
                    for (ulong val = 0; val < 10; val += 1)
                    {
                        yield return TestHelpers.Assert(eid, aid, tx, val);
                    }
                }
            }
        }
    }
}