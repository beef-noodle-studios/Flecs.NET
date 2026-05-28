using System.Runtime.InteropServices;
using static Flecs.NET.Bindings.flecs;

#pragma warning disable CS9265 // aspect ref fields are bound via unsafe binding, not C# assignment
#pragma warning disable CS0649 // component fields never assigned

namespace NoodleStudios.Flecs.Tests;

public sealed unsafe class AspectQueryTests
{
    // --- Owned storage ---

    [Test]
    public void Owned_aspect_reads_and_writes_across_multiple_tables()
    {
        using World world = new();
        world.Component<Position>();
        world.Component<Velocity>();
        Entity marker = world.CreateEntity();

        // Two archetypes that both match {Position, Velocity}: one bare, one with an
        // extra tag. Iterating must advance across both tables.
        Entity a = world.CreateEntity();
        world.Set(a, new Position { X = 1 });
        world.Set(a, new Velocity { X = 10 });
        Entity b = world.CreateEntity();
        world.Set(b, new Position { X = 2 });
        world.Set(b, new Velocity { X = 20 });
        world.Add(b, marker);

        var seen = new Dictionary<ulong, int>();
        using (DisposableQuery<OwnedAspect> query = world.CreateQuery<OwnedAspect>().BuildDisposable())
            foreach (ref readonly OwnedAspect m in query)
            {
                seen[m.Entity] = m.Position.X;
                m.Velocity.X += 100;
            }

        Assert.Multiple(() =>
        {
            Assert.That(seen, Has.Count.EqualTo(2), "both tables were iterated");
            Assert.That(seen[a], Is.EqualTo(1));
            Assert.That(seen[b], Is.EqualTo(2));
            Assert.That(world.Get<Velocity>(a).X, Is.EqualTo(110), "the ref write persisted");
            Assert.That(world.Get<Velocity>(b).X, Is.EqualTo(120));
        });
    }

    [Test]
    public void Owned_field_strides_by_the_padded_component_size()
    {
        using World world = new();
        world.Component<Padded>();

        // Three entities in one archetype share one table, so the owned column holds
        // three contiguous rows. Padded is 16 bytes (long + int + 4 bytes trailing
        // pad). If the per-row stride used the unpadded width (12) instead of the
        // component's true size, rows would overlap and these reads/writes corrupt.
        Entity a = world.CreateEntity();
        world.Set(a, new Padded { A = 1, B = 10 });
        Entity b = world.CreateEntity();
        world.Set(b, new Padded { A = 2, B = 20 });
        Entity c = world.CreateEntity();
        world.Set(c, new Padded { A = 3, B = 30 });

        var seen = new List<(long, int)>();
        using (DisposableQuery<PaddedAspect> query = world.CreateQuery<PaddedAspect>().BuildDisposable())
            foreach (ref readonly PaddedAspect m in query)
            {
                seen.Add((m.Padded.A, m.Padded.B));
                m.Padded.A += 100;
            }

        Assert.Multiple(() =>
        {
            Assert.That(seen, Is.EquivalentTo(new[] { (1L, 10), (2L, 20), (3L, 30) }),
                "each row reads its own values, so the stride spans the full padded size");
            Assert.That(world.Get<Padded>(a).A, Is.EqualTo(101));
            Assert.That(world.Get<Padded>(b).A, Is.EqualTo(102));
            Assert.That(world.Get<Padded>(c).A, Is.EqualTo(103));
        });
    }

    [Test]
    public void Entity_slot_binds_the_matched_entity()
    {
        using World world = new();
        world.Component<Position>();
        Entity a = world.CreateEntity();
        world.Set(a, new Position { X = 1 });
        Entity b = world.CreateEntity();
        world.Set(b, new Position { X = 2 });

        var matched = new List<ulong>();
        using (DisposableQuery<EntityAndPosition> query = world.CreateQuery<EntityAndPosition>().BuildDisposable())
            foreach (ref readonly EntityAndPosition m in query)
                matched.Add(m.Entity);

        Assert.That(matched, Is.EquivalentTo(new[] { (ulong)a, (ulong)b }));
    }

    [Test]
    public void Cached_typed_query_iterates_and_can_be_re_iterated()
    {
        using World world = new();
        world.Component<Position>();
        world.Set(world.CreateEntity(), new Position { X = 5 });
        world.Set(world.CreateEntity(), new Position { X = 7 });

        Query<EntityAndPosition> query = world.CreateQuery<EntityAndPosition>().BuildCached();
        try
        {
            Assert.That(Collect(query), Is.EquivalentTo(new[] { 5, 7 }));
            Assert.That(Collect(query), Is.EquivalentTo(new[] { 5, 7 }), "a cached query re-iterates");
        }
        finally
        {
            world.DestroyQuery(query.Untyped);
        }

        static List<int> Collect(Query<EntityAndPosition> q)
        {
            var values = new List<int>();
            foreach (ref readonly EntityAndPosition m in q)
                values.Add(m.Position.X);
            return values;
        }
    }

    // --- Shared storage and incidental-shared-write guard ---

    [Test]
    public void Shared_field_reads_the_inherited_value()
    {
        using World world = new();
        world.Component<Inherited>();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 42 });
        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);

        var instanceValues = new List<int>();
        using (DisposableQuery<ReadInherited> query = world.CreateQuery<ReadInherited>().BuildDisposable())
            foreach (ref readonly ReadInherited m in query)
                if ((ulong)m.Entity == (ulong)instance)
                    instanceValues.Add(m.Inherited.Value);

        Assert.That(instanceValues, Is.EquivalentTo(new[] { 42 }), "the instance reads the base's value");
    }

    [Test]
    public void Up_writable_field_on_a_shared_table_mutates_the_source()
    {
        using World world = new();
        world.Component<Position>();
        world.Component<Velocity>();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Velocity { X = 7 });
        Entity child = world.CreateEntity();
        world.Set(child, new Position { X = 1 });
        world.AddChildOf(child, parent);

        // [Up] ref Velocity is deliberately shared, so writing through it is allowed
        // and mutates the parent. The incidental-shared-write guard exempts it.
        Assert.DoesNotThrow(() =>
        {
            using DisposableQuery<UpWritableVelocity> query =
                world.CreateQuery<UpWritableVelocity>().BuildDisposable();
            foreach (ref readonly UpWritableVelocity m in query)
                m.Velocity.X += 100;
        });

        Assert.That(world.Get<Velocity>(parent).X, Is.EqualTo(107), "the write landed on the source");
    }

    [Test]
    public void Self_attribute_excludes_shared_matches_so_the_write_is_safe()
    {
        using World world = new();
        world.Component<Inherited>();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 7 }); // the base owns it
        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity); // reads it as a shared field

        var matched = new List<ulong>();
        Assert.DoesNotThrow(() =>
        {
            using DisposableQuery<SelfWritableInherited> query =
                world.CreateQuery<SelfWritableInherited>().BuildDisposable();
            foreach (ref readonly SelfWritableInherited m in query)
            {
                matched.Add(m.Entity);
                m.Inherited.Value += 1;
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(matched, Does.Not.Contain((ulong)instance), "[Self] excludes the inherit-only instance");
            Assert.That(matched, Does.Contain((ulong)baseEntity));
            Assert.That(world.Get<Inherited>(baseEntity).Value, Is.EqualTo(8), "the self write landed on the base");
        });
    }

#if DEBUG
    [Test]
    public void Writing_a_self_field_on_an_inherited_table_throws_in_debug()
    {
        using World world = new();
        world.Component<Inherited>();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 42 });
        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);

        // WriteInherited carries a writable self-sourced field with no [Self], so it
        // matches the instance's inherited table. Binding that row trips the guard.
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using DisposableQuery<WriteInherited> query =
                world.CreateQuery<WriteInherited>().BuildDisposable();
            foreach (ref readonly WriteInherited _ in query) { }
        })!;

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Does.Contain("Inherited"));
            Assert.That(ex.Message, Does.Contain("inherited"));
            Assert.That(ex.Message, Does.Contain("[Self]"));
        });
    }
#endif

    // --- Sparse storage ---

    [Test]
    public void Sparse_field_binds_distinct_values_per_row()
    {
        using World world = new();
        world.Component<SparseValue>();
        Entity a = world.CreateEntity();
        world.Set(a, new SparseValue { Value = 11 });
        Entity b = world.CreateEntity();
        world.Set(b, new SparseValue { Value = 22 });

        var seen = new List<int>();
        using (DisposableQuery<ReadSparse> query = world.CreateQuery<ReadSparse>().BuildDisposable())
            foreach (ref readonly ReadSparse m in query)
                seen.Add(m.Sparse.Value);

        Assert.That(seen, Is.EquivalentTo(new[] { 11, 22 }));
    }

    // --- Optional fields and Field.HasValue ---

    [Test]
    public void Optional_field_is_present_or_absent_per_table()
    {
        using World world = new();
        world.Component<Position>();
        world.Component<Velocity>();
        Entity withVelocity = world.CreateEntity();
        world.Set(withVelocity, new Position { X = 1 });
        world.Set(withVelocity, new Velocity { X = 7 });
        Entity withoutVelocity = world.CreateEntity();
        world.Set(withoutVelocity, new Position { X = 2 });

        bool sawPresent = false;
        bool sawAbsent = false;
        var velocities = new List<int>();
        using (DisposableQuery<OptionalVelocity> query = world.CreateQuery<OptionalVelocity>().BuildDisposable())
            foreach (ref readonly OptionalVelocity m in query)
            {
                if (Field.HasValue(in m.Velocity))
                {
                    sawPresent = true;
                    velocities.Add(m.Velocity.X);
                }
                else
                {
                    sawAbsent = true;
                }
            }

        Assert.Multiple(() =>
        {
            Assert.That(sawPresent, Is.True);
            Assert.That(sawAbsent, Is.True);
            Assert.That(velocities, Is.EquivalentTo(new[] { 7 }));
        });
    }

    [Test]
    public void Unmatched_optional_reports_no_value_without_dereferencing()
    {
        using World world = new();
        world.Component<Position>();
        world.Component<Velocity>();
        world.Set(world.CreateEntity(), new Position { X = 1 });

        bool matched = false;
        using (DisposableQuery<OptionalVelocity> query = world.CreateQuery<OptionalVelocity>().BuildDisposable())
            foreach (ref readonly OptionalVelocity m in query)
            {
                matched = true;
                // The optional did not match, so the field is a null reference.
                // HasValue must answer false without touching the null storage.
                Assert.That(Field.HasValue(in m.Velocity), Is.False);
            }

        Assert.That(matched, Is.True);
    }

    // --- Same-type self + up (two terms, two field indices) ---

    [Test]
    public void Self_and_up_of_the_same_component_bind_distinct_values()
    {
        using World world = new();
        world.Component<Position>();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Position { X = 100 });
        Entity child = world.CreateEntity();
        world.Set(child, new Position { X = 1 });
        world.AddChildOf(child, parent);

        int self = 0;
        int up = 0;
        using (DisposableQuery<SelfAndUpPosition> query = world.CreateQuery<SelfAndUpPosition>().BuildDisposable())
            foreach (ref readonly SelfAndUpPosition m in query)
                if ((ulong)m.Entity == (ulong)child)
                {
                    self = m.SelfPosition.X;
                    up = m.UpPosition.X;
                }

        Assert.Multiple(() =>
        {
            Assert.That(self, Is.EqualTo(1), "the child's own Position");
            Assert.That(up, Is.EqualTo(100), "the parent's Position via up");
        });
    }

    [Test]
    public void Up_declared_first_still_binds_distinct_values()
    {
        using World world = new();
        world.Component<Position>();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Position { X = 100 });
        Entity child = world.CreateEntity();
        world.Set(child, new Position { X = 1 });
        world.AddChildOf(child, parent);

        int self = 0;
        int up = 0;
        using (DisposableQuery<UpAndSelfPosition> query = world.CreateQuery<UpAndSelfPosition>().BuildDisposable())
            foreach (ref readonly UpAndSelfPosition m in query)
                if ((ulong)m.Entity == (ulong)child)
                {
                    self = m.SelfPosition.X;
                    up = m.UpPosition.X;
                }

        Assert.Multiple(() =>
        {
            Assert.That(self, Is.EqualTo(1));
            Assert.That(up, Is.EqualTo(100));
        });
    }

    // --- Sourcing attributes ---

    [Test]
    public void Singleton_field_reads_the_shared_singleton_value()
    {
        using World world = new();
        world.Component<Position>();
        Entity configEntity = world.Component<Config>();
        world.Set(configEntity, new Config { Level = 9 }); // the singleton instance
        world.Set(world.CreateEntity(), new Position { X = 1 });
        world.Set(world.CreateEntity(), new Position { X = 2 });

        var levels = new List<int>();
        var positions = new List<int>();
        using (DisposableQuery<PositionWithSingleton> query =
               world.CreateQuery<PositionWithSingleton>().BuildDisposable())
            foreach (ref readonly PositionWithSingleton m in query)
            {
                levels.Add(m.Config.Level);
                positions.Add(m.Position.X);
            }

        Assert.Multiple(() =>
        {
            Assert.That(positions, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(levels, Is.EquivalentTo(new[] { 9, 9 }), "every row reads the same singleton");
        });
    }

    [Test]
    public void Up_with_a_relationship_traverses_it_and_reads_the_source()
    {
        using World world = new();
        world.Component<Position>();
        world.Component<ParentLink>();
        Id parentLinkId = ComponentId<ParentLink>.GetId(world.Handle);
        ecs_add_id(world.Handle, parentLinkId, EcsTraversable);

        Entity parent = world.CreateEntity();
        world.Set(parent, new Position { X = 50 });
        Entity child = world.CreateEntity();
        world.AddPair(child, parentLinkId, parent);

        var values = new List<int>();
        using (DisposableQuery<UpRelationshipPosition> query =
               world.CreateQuery<UpRelationshipPosition>().BuildDisposable())
            foreach (ref readonly UpRelationshipPosition m in query)
                values.Add(m.Position.X);

        Assert.That(values, Is.EquivalentTo(new[] { 50 }), "the child reads its parent's Position via ParentLink");
    }

    // --- Struct-level matching attributes ---

    [Test]
    public void With_and_Without_filter_matches_without_disturbing_bindings()
    {
        using World world = new();
        world.Component<Position>();
        world.Component<Velocity>();
        world.Component<MarkerA>();
        world.Component<MarkerB>();

        Entity included = world.CreateEntity();
        world.Set(included, new Position { X = 1 });
        world.Set(included, new Velocity { X = 10 });
        world.Add<MarkerA>(included);

        Entity excludedByWithout = world.CreateEntity();
        world.Set(excludedByWithout, new Position { X = 2 });
        world.Set(excludedByWithout, new Velocity { X = 20 });
        world.Add<MarkerA>(excludedByWithout);
        world.Add<MarkerB>(excludedByWithout);

        Entity excludedByWith = world.CreateEntity();
        world.Set(excludedByWith, new Position { X = 3 });
        world.Set(excludedByWith, new Velocity { X = 30 });

        var matched = new List<ulong>();
        var positions = new List<int>();
        using (DisposableQuery<FilteredMovement> query = world.CreateQuery<FilteredMovement>().BuildDisposable())
            foreach (ref readonly FilteredMovement m in query)
            {
                matched.Add(m.Entity);
                positions.Add(m.Position.X);
                m.Velocity.X += 1;
            }

        Assert.Multiple(() =>
        {
            Assert.That(matched, Is.EquivalentTo(new[] { (ulong)included }));
            Assert.That(positions, Is.EquivalentTo(new[] { 1 }), "bindings still read the right field");
            Assert.That(world.Get<Velocity>(included).X, Is.EqualTo(11), "the write still lands");
        });
    }

    [Test]
    public void Any_group_matches_either_marker()
    {
        using World world = new();
        world.Component<Position>();
        world.Component<MarkerA>();
        world.Component<MarkerB>();
        world.Component<MarkerC>();

        Entity withA = world.CreateEntity();
        world.Set(withA, new Position { X = 1 });
        world.Add<MarkerA>(withA);
        Entity withB = world.CreateEntity();
        world.Set(withB, new Position { X = 2 });
        world.Add<MarkerB>(withB);
        Entity withC = world.CreateEntity();
        world.Set(withC, new Position { X = 3 });
        world.Add<MarkerC>(withC);

        var matched = new List<ulong>();
        using (DisposableQuery<AnyMarker> query = world.CreateQuery<AnyMarker>().BuildDisposable())
            foreach (ref readonly AnyMarker m in query)
                matched.Add(m.Entity);

        Assert.That(matched, Is.EquivalentTo(new[] { (ulong)withA, (ulong)withB }));
    }

    // --- TableView bound as an aspect field ---

    [Test]
    public void TableView_field_answers_the_same_questions_as_an_untyped_view()
    {
        using World world = new();
        Id positionId = world.Component<Position>();
        Entity likes = world.CreateEntity();
        Entity apples = world.CreateEntity();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.AddPair(e, likes, apples);

        bool matched = false;
        using (DisposableQuery<WithTable> query =
               world.CreateQuery<WithTable>().With(likes, apples).BuildDisposable())
            foreach (ref readonly WithTable m in query)
            {
                matched = true;

                // Read into locals so the assertion lambda does not capture the ref local.
                bool hasField = m.Table.HasField<Position>();
                bool hasSelf = m.Table.HasSelfField<Position>();
                bool hasShared = m.Table.HasSharedField<Position>();
                int positionField = m.Table.FindField<Position>();
                Id field0 = m.Table.GetFieldId(0);
                Id field1 = m.Table.GetFieldId(1);
                Entity pairTarget = m.Table.GetFieldTarget(1);
                Entity positionSource = m.Table.GetFieldSource<Position>();

                Assert.Multiple(() =>
                {
                    Assert.That(hasField, Is.True);
                    Assert.That(hasSelf, Is.True);
                    Assert.That(hasShared, Is.False);
                    Assert.That(positionField, Is.EqualTo(0));
                    Assert.That(field0, Is.EqualTo(positionId));
                    Assert.That(field1, Is.EqualTo(world.Pair(likes, apples)));
                    Assert.That(pairTarget, Is.EqualTo(apples));
                    Assert.That(positionSource, Is.EqualTo(Entity.None), "Position is self-bound");
                });
            }

        Assert.That(matched, Is.True);
    }

    [Test]
    public void TableView_field_reports_the_source_of_an_up_sourced_field()
    {
        using World world = new();
        world.Component<Position>();
        world.Component<Velocity>();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Velocity { X = 7 });
        Entity child = world.CreateEntity();
        world.Set(child, new Position { X = 1 });
        world.AddChildOf(child, parent);

        Entity source = Entity.None;
        bool sawShared = false;
        using (DisposableQuery<TableWithUpVelocity> query =
               world.CreateQuery<TableWithUpVelocity>().BuildDisposable())
            foreach (ref readonly TableWithUpVelocity m in query)
            {
                sawShared = m.Table.HasSharedField<Velocity>();
                source = m.Table.GetFieldSource<Velocity>(FieldScanMode.SharedOnly);
            }

        Assert.Multiple(() =>
        {
            Assert.That(sawShared, Is.True, "Velocity is inherited from the parent");
            Assert.That(source, Is.EqualTo(parent), "the up-sourced field's source is the parent");
        });
    }

    // --- Builder refinement after CreateQuery ---

    [Test]
    public void Refining_a_typed_query_after_creation_leaves_bindings_intact()
    {
        using World world = new();
        world.Component<Position>();
        world.Component<Velocity>();
        Entity onlyPosition = world.CreateEntity();
        world.Set(onlyPosition, new Position { X = 1 });
        Entity both = world.CreateEntity();
        world.Set(both, new Position { X = 2 });
        world.Set(both, new Velocity { X = 9 });

        var matched = new List<ulong>();
        var positions = new List<int>();
        using (DisposableQuery<EntityAndPosition> query =
               world.CreateQuery<EntityAndPosition>().Without<Velocity>().BuildDisposable())
            foreach (ref readonly EntityAndPosition m in query)
            {
                matched.Add(m.Entity);
                positions.Add(m.Position.X);
            }

        Assert.Multiple(() =>
        {
            Assert.That(matched, Is.EquivalentTo(new[] { (ulong)onlyPosition }), "Without narrowed the match");
            Assert.That(positions, Is.EquivalentTo(new[] { 1 }), "Position still binds to field 0");
        });
    }

    // --- Cached-only sourcing built uncached ---

    [Test]
    public void UpDescendantsFirst_aspect_built_uncached_throws()
    {
        using World world = new();
        world.Component<Position>();

        Assert.Throws<InvalidOperationException>(() =>
            world.CreateQuery<DescendantsFirstAspect>().BuildUncached());
    }

    // --- Test components ---

    internal struct Position { public int X; public int Y; }
    internal struct Velocity { public int X; public int Y; }
    internal struct Config { public int Level; }
    internal struct Padded { public long A; public int B; } // 16 bytes: 8 + 4 + 4 trailing pad
    internal struct MarkerA { }
    internal struct MarkerB { }
    internal struct MarkerC { }
    internal struct ParentLink { }

    [Sparse]
    internal struct SparseValue { public int Value; }

    [OnInstantiate(Instantiate.Inherit)]
    internal struct Inherited { public int Value; }

    // --- Test aspect shapes ---

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct OwnedAspect : IAspect
    {
        public Entity Entity;
        public ref readonly Position Position;
        public ref Velocity Velocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct EntityAndPosition : IAspect
    {
        public Entity Entity;
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct PaddedAspect : IAspect
    {
        public ref Padded Padded;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct ReadInherited : IAspect
    {
        public Entity Entity;
        public ref readonly Inherited Inherited;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct WriteInherited : IAspect
    {
        public ref Inherited Inherited;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct UpWritableVelocity : IAspect
    {
        public ref readonly Position Position;

        [Up]
        public ref Velocity Velocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct SelfWritableInherited : IAspect
    {
        public Entity Entity;

        [Self]
        public ref Inherited Inherited;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct ReadSparse : IAspect
    {
        public ref readonly SparseValue Sparse;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct OptionalVelocity : IAspect
    {
        public ref readonly Position Position;

        [Optional]
        public ref readonly Velocity Velocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct SelfAndUpPosition : IAspect
    {
        public Entity Entity;
        public ref readonly Position SelfPosition;

        [Up]
        public ref readonly Position UpPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct UpAndSelfPosition : IAspect
    {
        public Entity Entity;

        [Up]
        public ref readonly Position UpPosition;

        public ref readonly Position SelfPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct PositionWithSingleton : IAspect
    {
        public ref readonly Position Position;

        [Singleton]
        public ref readonly Config Config;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct UpRelationshipPosition : IAspect
    {
        [Up(typeof(ParentLink))]
        public ref readonly Position Position;
    }

    [With(typeof(MarkerA))]
    [Without(typeof(MarkerB))]
    [StructLayout(LayoutKind.Sequential)]
    internal ref struct FilteredMovement : IAspect
    {
        public Entity Entity;
        public ref readonly Position Position;
        public ref Velocity Velocity;
    }

    [Any(typeof(MarkerA), typeof(MarkerB))]
    [StructLayout(LayoutKind.Sequential)]
    internal ref struct AnyMarker : IAspect
    {
        public Entity Entity;
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct WithTable : IAspect
    {
        public TableView Table;
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct TableWithUpVelocity : IAspect
    {
        public TableView Table;
        public ref readonly Position Position;

        [Up]
        public ref readonly Velocity Velocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct DescendantsFirstAspect : IAspect
    {
        [UpDescendantsFirst]
        public ref readonly Position Position;
    }
}

#pragma warning restore CS0649
#pragma warning restore CS9265
