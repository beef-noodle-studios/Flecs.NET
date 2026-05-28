using System.Linq;
using System.Runtime.InteropServices;
using static Flecs.NET.Bindings.flecs;

#pragma warning disable CS9265 // aspect ref fields are bound via unsafe binding, not C# assignment
#pragma warning disable CS0649 // component fields never assigned

namespace NoodleStudios.Flecs.Tests;

public sealed unsafe class AspectBuilderTests
{
    [Test]
    public void Plain_aspect_seeds_three_accessor_terms_at_field_indices_zero_one_two()
    {
        using World world = new();
        // Components must be registered before the aspect query is built.
        world.Set(world.CreateEntity(), new Position());
        world.Set(world.CreateEntity(), new Velocity());
        world.Set(world.CreateEntity(), new Mass());

        Query<ThreeFieldAspect> query = world.CreateQuery<ThreeFieldAspect>().BuildUncached();
        try
        {
            ecs_term_t* terms = (ecs_term_t*)(&query.Untyped.Handle->terms);
            int[] map = query.SlotToTermIndex;

            Assert.Multiple(() =>
            {
                Assert.That(map, Has.Length.EqualTo(3));
                Assert.That(terms[map[0]].field_index, Is.EqualTo(0));
                Assert.That(terms[map[1]].field_index, Is.EqualTo(1));
                Assert.That(terms[map[2]].field_index, Is.EqualTo(2));
            });
        }
        finally
        {
            world.DestroyQuery(query.Untyped);
        }
    }

    [Test]
    public void Struct_With_and_Without_attributes_do_not_disturb_the_slot_map()
    {
        using World world = new();
        // Register everything used by the aspect, including the struct-level marks.
        world.Set(world.CreateEntity(), new Position());
        world.Set(world.CreateEntity(), new Velocity());
        world.Set(world.CreateEntity(), new MarkerA());
        world.Set(world.CreateEntity(), new MarkerB());

        Query<WithAndWithoutAspect> query = world.CreateQuery<WithAndWithoutAspect>().BuildUncached();
        try
        {
            ecs_term_t* terms = (ecs_term_t*)(&query.Untyped.Handle->terms);
            int[] map = query.SlotToTermIndex;
            int termCount = query.Untyped.Handle->term_count;

            Assert.Multiple(() =>
            {
                Assert.That(map, Has.Length.EqualTo(2), "two accessor slots");
                Assert.That(map[0], Is.EqualTo(0), "Position is the first seeded term");
                Assert.That(map[1], Is.EqualTo(1), "Velocity is the second seeded term");
                Assert.That(terms[map[0]].field_index, Is.EqualTo(0));
                Assert.That(terms[map[1]].field_index, Is.EqualTo(1));
                Assert.That(termCount, Is.EqualTo(4), "two accessor + one [With] + one [Without]");
            });
        }
        finally
        {
            world.DestroyQuery(query.Untyped);
        }
    }

    [Test]
    public void Self_and_Up_on_same_component_get_distinct_term_and_field_indices()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position());

        Query<SelfPlusUpAspect> query = world.CreateQuery<SelfPlusUpAspect>().BuildUncached();
        try
        {
            ecs_term_t* terms = (ecs_term_t*)(&query.Untyped.Handle->terms);
            int[] map = query.SlotToTermIndex;

            int selfTerm = map[0];
            int upTerm = map[1];

            Assert.Multiple(() =>
            {
                Assert.That(selfTerm, Is.Not.EqualTo(upTerm), "distinct term indices");
                Assert.That(terms[selfTerm].field_index, Is.Not.EqualTo(terms[upTerm].field_index),
                    "distinct field indices");
                Assert.That(terms[selfTerm].src.id & EcsUp, Is.Zero, "self term is not up-sourced");
                Assert.That(terms[upTerm].src.id & EcsUp, Is.Not.Zero, "up term carries the EcsUp flag");
            });
        }
        finally
        {
            world.DestroyQuery(query.Untyped);
        }
    }

    [Test]
    public void Up_declared_first_still_gets_a_distinct_field_index()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position());

        Query<UpFirstAspect> query = world.CreateQuery<UpFirstAspect>().BuildUncached();
        try
        {
            ecs_term_t* terms = (ecs_term_t*)(&query.Untyped.Handle->terms);
            int[] map = query.SlotToTermIndex;

            int upTerm = map[0];
            int selfTerm = map[1];

            Assert.Multiple(() =>
            {
                Assert.That(terms[upTerm].field_index, Is.Not.EqualTo(terms[selfTerm].field_index));
                Assert.That(terms[upTerm].src.id & EcsUp, Is.Not.Zero);
                Assert.That(terms[selfTerm].src.id & EcsUp, Is.Zero);
            });
        }
        finally
        {
            world.DestroyQuery(query.Untyped);
        }
    }

    [Test]
    public void Up_with_relationship_attribute_lowers_to_an_Up_term_with_trav_set()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position());
        // The relationship type must be registered as a traversable component.
        world.Component<ParentLink>();
        Id parentLinkId = ComponentId<ParentLink>.GetId(world.Handle);
        ecs_add_id(world.Handle, parentLinkId, EcsTraversable);

        Query<UpRelationshipAspect> query = world.CreateQuery<UpRelationshipAspect>().BuildUncached();
        try
        {
            ecs_term_t* terms = (ecs_term_t*)(&query.Untyped.Handle->terms);
            int term = query.SlotToTermIndex[0];

            Assert.Multiple(() =>
            {
                Assert.That(terms[term].src.id & EcsUp, Is.Not.Zero, "Up flag is set");
                Assert.That(terms[term].trav, Is.EqualTo(parentLinkId.Value),
                    "trav is the registered relationship's id");
            });
        }
        finally
        {
            world.DestroyQuery(query.Untyped);
        }
    }

    [Test]
    public void Singleton_attribute_lowers_to_a_fixed_source_at_the_component_id()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Config());
        Id configId = ComponentId<Config>.GetId(world.Handle);

        Query<SingletonAspect> query = world.CreateQuery<SingletonAspect>().BuildUncached();
        try
        {
            ecs_term_t* terms = (ecs_term_t*)(&query.Untyped.Handle->terms);
            int term = query.SlotToTermIndex[0];

            ulong srcId = terms[term].src.id;
            const ulong AllFlags = EcsSelf | EcsUp | EcsCascade | EcsDesc | EcsIsEntity;
            Assert.Multiple(() =>
            {
                Assert.That(srcId & EcsIsEntity, Is.Not.Zero, "fixed-entity source");
                Assert.That(srcId & EcsUp, Is.Zero, "Up flag is not set");
                Assert.That(srcId & ~AllFlags, Is.EqualTo(configId.Value),
                    "entity portion is the component's id");
            });
        }
        finally
        {
            world.DestroyQuery(query.Untyped);
        }
    }

    [Test]
    public void Self_attribute_sets_the_EcsSelf_flag_on_the_term_source()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Velocity());

        Query<SelfAttributeAspect> query = world.CreateQuery<SelfAttributeAspect>().BuildUncached();
        try
        {
            ecs_term_t* terms = (ecs_term_t*)(&query.Untyped.Handle->terms);
            int term = query.SlotToTermIndex[0];

            Assert.That(terms[term].src.id & EcsSelf, Is.Not.Zero);
        }
        finally
        {
            world.DestroyQuery(query.Untyped);
        }
    }

    [Test]
    public void Unregistered_component_throws_a_clear_error()
    {
        using World world = new();
        // Position is never registered: no Set/Component for it.
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            world.CreateQuery<ThreeFieldAspect>())!;

        Assert.That(ex.Message, Does.Contain("Position"));
        Assert.That(ex.Message, Does.Contain("not registered"));
    }

    [Test]
    public void Cached_only_aspect_built_uncached_throws()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position());

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            world.CreateQuery<CachedOnlyAspect>().BuildUncached())!;

        Assert.That(ex.Message, Does.Contain("Failed to build query"));
    }

    // --- Test components and aspect shapes ---

    internal struct Position { public int X; public int Y; }
    internal struct Velocity { public int X; public int Y; }
    internal struct Mass { public float Value; }
    internal struct Config { public int Level; }
    internal struct MarkerA { }
    internal struct MarkerB { }
    internal struct ParentLink { }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct ThreeFieldAspect : IAspect
    {
        public ref readonly Position Position;
        public ref Velocity Velocity;
        public ref readonly Mass Mass;
    }

    [With(typeof(MarkerA))]
    [Without(typeof(MarkerB))]
    [StructLayout(LayoutKind.Sequential)]
    internal ref struct WithAndWithoutAspect : IAspect
    {
        public ref readonly Position Position;
        public ref Velocity Velocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct SelfPlusUpAspect : IAspect
    {
        public ref readonly Position SelfPosition;

        [Up]
        public ref readonly Position UpPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct UpFirstAspect : IAspect
    {
        [Up]
        public ref readonly Position UpPosition;

        public ref readonly Position SelfPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct UpRelationshipAspect : IAspect
    {
        [Up(typeof(ParentLink))]
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct SingletonAspect : IAspect
    {
        [Singleton]
        public ref readonly Config Config;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct SelfAttributeAspect : IAspect
    {
        [Self]
        public ref Velocity Velocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct CachedOnlyAspect : IAspect
    {
        [UpAncestorsFirst]
        public ref readonly Position Position;
    }
}

#pragma warning restore CS0649
#pragma warning restore CS9265
