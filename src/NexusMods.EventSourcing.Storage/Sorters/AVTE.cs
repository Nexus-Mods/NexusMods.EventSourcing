﻿using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

namespace NexusMods.EventSourcing.Storage.Sorters;

public class AVTE(AttributeRegistry registry) : IDatomComparator
{
    public SortOrders SortOrder => SortOrders.AVTE;
    public IAttributeRegistry AttributeRegistry => registry;

    public int Compare(in Datom x, in Datom y)
    {
        var cmp = x.A.CompareTo(y.A);
        if (cmp != 0) return cmp;

        cmp = registry.CompareValues(x, y);
        if (cmp != 0) return cmp;

        cmp = x.T.CompareTo(y.T);
        if (cmp != 0) return -cmp;

        return x.E.CompareTo(y.E);
    }

    public IComparer<int> MakeComparer(IDatomResult datoms)
    {
        return new Comparer(registry, datoms);
    }

    private class Comparer(AttributeRegistry registry, IDatomResult datoms) : IComparer<int>
    {
        public int Compare(int a, int b)
        {
            var cmp = datoms.GetAttributeId(a).CompareTo(datoms.GetAttributeId(b));
            if (cmp != 0) return cmp;

            cmp = registry.CompareValues(datoms.GetAttributeId(a), datoms.GetValue(a), datoms.GetValue(b));
            if (cmp != 0) return cmp;

            // Reverse the comparison of transaction ids to get the latest transaction first
            cmp = datoms.GetTransactionId(a).CompareTo(datoms.GetTransactionId(b));
            if (cmp != 0) return -cmp;

            return datoms.GetEntityId(a).CompareTo(datoms.GetEntityId(b));
        }
    }
}
