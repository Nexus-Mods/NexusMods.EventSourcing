using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

[Entity("ACB7AF43-4FB2-4E1A-8C32-7CF7D912A911", 0)]
public class Mod(IEntityContext context, EntityId<Mod> id) : AEntity(context, id)
{
    public Loadout Loadout => _loadout.Get(this);
    internal static readonly EntityAttributeDefinition<Mod, Loadout> _loadout = new(nameof(Loadout));

    /// <summary>
    /// The human readable name of the mod.
    /// </summary>
    public string Name => _name.Get(this);
    internal static readonly ScalarAttribute<Mod, string> _name = new(nameof(Name));

    /// <summary>
    /// The enabled state of the mod.
    /// </summary>
    public bool Enabled => _enabled.Get(this);
    internal static readonly ScalarAttribute<Mod, bool> _enabled = new(nameof(Enabled));

    /// <summary>
    /// The Collection the mod is in, if any
    /// </summary>
    public Collection Collection => _collection.Get(this);
    internal static readonly EntityAttributeDefinition<Mod, Collection> _collection = new(nameof(Collection));

}
