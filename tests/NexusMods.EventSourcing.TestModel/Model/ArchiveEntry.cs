﻿using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

[Entity("0FFCC422-7281-44C4-A4E6-32ACE31C00AF", 0)]
public class ArchiveEntry(IEntityContext context, EntityId<ArchiveEntry> id) : AEntity<ArchiveEntry>(context, id)
{
    public string Name => _name.Get(this);
    internal static readonly ScalarAttribute<ArchiveEntry, string> _name = new(nameof(Name));

    [Indexed("495CA9CB-F803-4577-853E-AA9AC196D3EA")]
    public ulong Size => _size.Get(this);
    internal static readonly ScalarAttribute<ArchiveEntry, ulong> _size = new(nameof(Size));


    [Indexed("9FD7C60A-B71E-4B3B-BB68-965312553D69")]
    public ulong Hash => _hash.Get(this);
    internal static readonly ScalarAttribute<ArchiveEntry, ulong> _hash = new(nameof(Hash));

    public Archive Archive => _archive.Get(this);
    internal static readonly EntityAttributeDefinition<ArchiveEntry, Archive> _archive = new(nameof(Archive));
}
