﻿using System;
using NexusMods.EventSourcing.Abstractions.DatomIterators;
using NexusMods.EventSourcing.Abstractions.Internals;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
///     Reverses the order of the iterator so that a .Next will move backwards
/// </summary>
public class ReverseIterator<TParent> : IIterator where TParent : IIterator
{
    private readonly TParent _parent;

    internal ReverseIterator(TParent parent)
    {
        _parent = parent;
    }

    /// <inheritdoc />
    public bool Valid => _parent.Valid;

    /// <inheritdoc />
    public void Next()
    {
        _parent.Prev();
    }

    /// <inheritdoc />
    public void Prev()
    {
        _parent.Next();
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> Current => _parent.Current;

    /// <inheritdoc />
    public IAttributeRegistry Registry => _parent.Registry;
}
