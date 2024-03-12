﻿using System;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

/// <summary>
/// A unpacked blob column. This column
/// </summary>
public interface IUnpacked : IReadable
{

    /// <summary>
    /// Get the data span for the column.
    /// </summary>
    public ReadOnlySpan<byte> Span { get; }

    /// <summary>
    /// A span of offsets into the column for each value.
    /// </summary>
    public ULongColumns.IUnpacked Offsets { get; }

    /// <summary>
    /// A span of lengths for each value in the column.
    /// </summary>
    public ULongColumns.IUnpacked Lengths { get; }


    public IReadable Pack()
    {
        return new BlobPackedColumn
        {
            Count = Count,
            Offsets = (ULongPackedColumn)Offsets.Pack(),
            Lengths = (ULongPackedColumn)Lengths.Pack(),
            Data = Span.ToArray()
        };
    }
}
