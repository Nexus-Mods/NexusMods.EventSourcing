﻿using System;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

/// <summary>
/// An appendable blob column. This column allows for appending of new spans of data.
/// </summary>
public interface IAppendable
{
    /// <summary>
    /// Add the specified span to the column.
    /// </summary>
    public void Append(ReadOnlySpan<byte> span);

    /// <summary>
    /// Setter for items in the column.
    /// </summary>
    /// <param name="offset"></param>
    public ReadOnlySpan<byte> this[int offset] { set; }
}
