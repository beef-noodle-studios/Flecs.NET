using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs.Tests;

public sealed class ComponentTraitTests
{
    [Test]
    public void Bare_marker_attributes_each_add_their_built_in_id()
    {
        using World world = new();

        Entity component = world.Component<MarkerBag>();

        Assert.Multiple(() =>
        {
            Assert.That(world.Has(component, EcsSparse), Is.True);
            Assert.That(world.Has(component, EcsFinal), Is.True);
            Assert.That(world.Has(component, EcsTransitive), Is.True);
            Assert.That(world.Has(component, EcsExclusive), Is.True);
            Assert.That(world.Has(component, EcsTrait), Is.True);
        });
    }

    [Test]
    public void OnInstantiate_attribute_adds_only_the_requested_instantiate_mode()
    {
        using World world = new();

        Entity component = world.Component<InheritedComponent>();

        Assert.Multiple(() =>
        {
            Assert.That(world.HasPair(component, EcsOnInstantiate, EcsInherit), Is.True);
            Assert.That(world.HasPair(component, EcsOnInstantiate, EcsOverride), Is.False);
            Assert.That(world.HasPair(component, EcsOnInstantiate, EcsDontInherit), Is.False);
        });
    }

    [Test]
    public void Custom_single_trait_adds_the_referenced_type_id()
    {
        using World world = new();

        Entity component = world.Component<TaggedComponent>();
        Entity serializable = world.Component<Serializable>();

        Assert.That(world.Has(component, serializable), Is.True);
    }

    [Test]
    public void Custom_pair_trait_adds_the_relationship_target_pair()
    {
        using World world = new();

        Entity component = world.Component<PairedComponent>();
        Entity dependsOn = world.Component<DependsOn>();
        Entity rendering = world.Component<Rendering>();

        Assert.That(world.HasPair(component, dependsOn, rendering), Is.True);
    }

    [Test]
    public void Mutually_referencing_custom_traits_register_without_recursing_forever()
    {
        using World world = new();

        Entity a = world.Component<CycleA>();
        Entity b = world.Component<CycleB>();

        Assert.Multiple(() =>
        {
            Assert.That(world.Has(a, b), Is.True);
            Assert.That(world.Has(b, a), Is.True);
        });
    }

    [Test]
    public void Traits_are_applied_independently_in_each_world()
    {
        using World first = new();
        using World second = new();

        Assert.Multiple(() =>
        {
            Assert.That(first.Has(first.Component<MultiWorldSparse>(), EcsSparse), Is.True);
            Assert.That(second.Has(second.Component<MultiWorldSparse>(), EcsSparse), Is.True);
        });
    }

#if DEBUG
    [Test]
    public void Conflicting_storage_traits_throw_in_debug_builds()
    {
        using World world = new();

        Assert.That(
            world.Component<SparseUnionConflict>,
            Throws.InstanceOf<InvalidOperationException>());
    }
#endif

    [Sparse]
    [Final]
    [Transitive]
    [Exclusive]
    [IsTrait]
    private struct MarkerBag
    {
        public int Value;
    }

    [OnInstantiate(Instantiate.Inherit)]
    private struct InheritedComponent;

    private struct Serializable;

    [Trait<Serializable>]
    private struct TaggedComponent;

    private struct DependsOn;

    private struct Rendering;

    [Trait<DependsOn, Rendering>]
    private struct PairedComponent;

    [Trait<CycleB>]
    private struct CycleA;

    [Trait<CycleA>]
    private struct CycleB;

    [Sparse]
    private struct MultiWorldSparse
    {
        public int Value;
    }

    [Sparse]
    [Union]
    private struct SparseUnionConflict
    {
        public int Value;
    }
}

