﻿using System.Collections.Generic;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Abstractions;

public interface IIndexChunk : IDataChunk
{
    /// <summary>
    /// The child nodes of this index chunk, each child's last datom can be accessed
    /// by using the accessors in the IDataChunk interface
    /// </summary>
    public IEnumerable<IDataChunk> Children { get; }

    public IColumn<int> ChildCounts { get; }
    IDatomComparator Comparator { get; }
}
