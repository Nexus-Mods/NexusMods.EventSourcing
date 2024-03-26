﻿using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IIndex {
    int Compare(ReadOnlySpan<byte> readOnlySpan, ReadOnlySpan<byte> readOnlySpan1);
    IDatomIterator GetIterator();
    void Delete(IWriteBatch batch, ReadOnlySpan<byte> span);
    void Put(IWriteBatch batch, ReadOnlySpan<byte> span);
}

