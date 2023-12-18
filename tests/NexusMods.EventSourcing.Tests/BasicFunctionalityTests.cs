using System.Collections.Specialized;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Tests;

public class BasicFunctionalityTests
{
    private readonly IEntityContext _ctx;
    public BasicFunctionalityTests(EventSerializer serializer)
    {
        var store = new InMemoryEventStore<EventSerializer>(serializer);
        _ctx = new EntityContext(store);
    }



    [Fact]
    public void CanSetupBasicLoadout()
    {
        var createEvent = CreateLoadout.Create("Test");
        _ctx.Add(createEvent);
        var loadout = _ctx.Get(createEvent.Id);
        loadout.Should().NotBeNull();
        loadout.Name.Should().Be("Test");
    }

    [Fact]
    public void ChangingPropertyChangesTheValue()
    {
        var createEvent = CreateLoadout.Create("Test");
        _ctx.Add(createEvent);
        var loadout = _ctx.Get(createEvent.Id);
        loadout.Name.Should().Be("Test");

        _ctx.Add(new RenameLoadout(createEvent.Id, "New Name"));
        loadout.Name.Should().Be("New Name");
    }

    [Fact]
    public void CanLinkEntities()
    {
        var loadoutEvent = CreateLoadout.Create("Test");
        _ctx.Add(loadoutEvent);
        var loadout = _ctx.Get(loadoutEvent.Id);
        loadout.Name.Should().Be("Test");

        var modEvent = AddMod.Create("First Mod", loadoutEvent.Id);
        _ctx.Add(modEvent);

        loadout.Mods.First().Name.Should().Be("First Mod");
        loadout.Mods.First().Loadout.Should().BeSameAs(loadout);
    }


    [Fact]
    public void CanDeleteEntities()
    {
        var loadoutEvent = CreateLoadout.Create("Test");
        _ctx.Add(loadoutEvent);
        var loadout = _ctx.Get(loadoutEvent.Id);
        loadout.Name.Should().Be("Test");

        var modEvent1 = AddMod.Create("First Mod", loadoutEvent.Id);
        _ctx.Add(modEvent1);

        var modEvent2 = AddMod.Create("Second Mod", loadoutEvent.Id);
        _ctx.Add(modEvent2);

        loadout.Mods.Count().Should().Be(2);

        _ctx.Add(new DeleteMod(modEvent1.ModId, loadoutEvent.Id));

        loadout.Mods.Count().Should().Be(1);

        loadout.Mods.First().Name.Should().Be("Second Mod");
    }

    [Fact]
    public void CanGetSingletonEntities()
    {
        var entity = _ctx.Get<LoadoutRegistry>();
        entity.Should().NotBeNull();
    }

    [Fact]
    public void UpdatingAValueCallesNotifyPropertyChanged()
    {
        var createEvent = CreateLoadout.Create("Test");
        _ctx.Add(createEvent);
        var loadout = _ctx.Get(createEvent.Id);
        loadout.Name.Should().Be("Test");

        var called = false;
        loadout.PropertyChanged += (sender, args) =>
        {
            called = true;
            args.PropertyName.Should().Be(nameof(Loadout.Name));
        };

        _ctx.Add(new RenameLoadout(createEvent.Id, "New Name"));
        loadout.Name.Should().Be("New Name");
        called.Should().BeTrue();
    }

    [Fact]
    public void EntityCollectionsAreObservable()
    {
        var loadouts = _ctx.Get<LoadoutRegistry>();
        _ctx.Add(CreateLoadout.Create("Test"));
        loadouts.Loadouts.Should().NotBeEmpty();

        var called = false;
        ((INotifyCollectionChanged)loadouts.Loadouts).CollectionChanged += (sender, args) =>
        {
            called = true;
            args.Action.Should().Be(NotifyCollectionChangedAction.Add);
            args.NewItems.Should().NotBeNull();
            args.NewItems!.Count.Should().Be(1);
            args.NewItems!.OfType<Loadout>().First().Name.Should().Be("Test2");
        };

        _ctx.Add(CreateLoadout.Create("Test2"));
        called.Should().BeTrue();

        loadouts.Loadouts.Count.Should().Be(2);
        loadouts.Loadouts.FirstOrDefault(l => l.Name == "Test2").Should().NotBeNull();
    }
}
