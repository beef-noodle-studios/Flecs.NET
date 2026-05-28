using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs.Tests;

public sealed class QueryTests
{
    // --- Core matching and reading ---

    [Test]
    public void With_matches_entities_that_have_the_component()
    {
        using World world = new();
        Entity a = world.CreateEntity();
        Entity b = world.CreateEntity();
        world.Set(a, new Position { X = 1 });
        world.Set(b, new Position { X = 3 });

        Query query = world.CreateQuery().With<Position>().BuildUncached();

        var seen = new List<int>();
        foreach (TableView table in query)
        {
            ReadOnlySpan<Position> positions = table.GetFieldSpan<Position>();
            for (int row = 0; row < table.Count; row++)
                seen.Add(positions[row].X);
        }

        Assert.That(seen, Is.EquivalentTo(new[] { 1, 3 }));
        world.DestroyQuery(query);
    }

    [Test]
    public void With_multiple_matches_only_entities_that_have_all()
    {
        using World world = new();
        Entity both = world.CreateEntity();
        Entity onlyPosition = world.CreateEntity();
        world.Set(both, new Position { X = 10 });
        world.Set(both, new Velocity { X = 1 });
        world.Set(onlyPosition, new Position { X = 20 });

        Query query = world.CreateQuery().With<Position>().With<Velocity>().BuildUncached();

        var seen = new List<int>();
        foreach (TableView table in query)
        {
            ReadOnlySpan<Position> positions = table.GetFieldSpan<Position>();
            for (int row = 0; row < table.Count; row++)
                seen.Add(positions[row].X);
        }

        Assert.That(seen, Is.EquivalentTo(new[] { 10 }));
        world.DestroyQuery(query);
    }

    [Test]
    public void Writes_through_a_field_span_persist()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });

        using (DisposableQuery query = world.CreateQuery().With<Position>().BuildDisposable())
            foreach (TableView table in query)
            {
                Span<Position> positions = table.GetFieldSpanMut<Position>();
                for (int row = 0; row < table.Count; row++)
                    positions[row].X += 100;
            }

        Assert.That(world.Get<Position>(e).X, Is.EqualTo(101));
    }

    [Test]
    public void Without_excludes_entities_that_have_the_component()
    {
        using World world = new();
        Entity both = world.CreateEntity();
        Entity onlyPosition = world.CreateEntity();
        world.Set(both, new Position { X = 10 });
        world.Set(both, new Velocity { X = 1 });
        world.Set(onlyPosition, new Position { X = 20 });

        Query query = world.CreateQuery().With<Position>().Without<Velocity>().BuildUncached();

        var seen = new List<int>();
        foreach (TableView table in query)
        {
            ReadOnlySpan<Position> positions = table.GetFieldSpan<Position>();
            for (int row = 0; row < table.Count; row++)
                seen.Add(positions[row].X);
        }

        Assert.That(seen, Is.EquivalentTo(new[] { 20 }));
        world.DestroyQuery(query);
    }

    [Test]
    public void Optional_carries_data_only_on_tables_that_have_it()
    {
        using World world = new();
        Entity withVelocity = world.CreateEntity();
        Entity withoutVelocity = world.CreateEntity();
        world.Set(withVelocity, new Position { X = 1 });
        world.Set(withVelocity, new Velocity { X = 7 });
        world.Set(withoutVelocity, new Position { X = 2 });

        Query query = world.CreateQuery().With<Position>().Optional<Velocity>().BuildUncached();

        bool sawPresent = false;
        bool sawAbsent = false;
        var velocities = new List<int>();
        foreach (TableView table in query)
        {
            if (table.HasField<Velocity>())
            {
                sawPresent = true;
                Assert.That(table.TryGetFieldSpan(out ReadOnlySpan<Velocity> v), Is.True);
                for (int row = 0; row < table.Count; row++)
                    velocities.Add(v[row].X);
            }
            else
            {
                sawAbsent = true;
                Assert.That(table.TryGetFieldSpan(out ReadOnlySpan<Velocity> _), Is.False);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(sawPresent, Is.True, "a table where the optional matched");
            Assert.That(sawAbsent, Is.True, "a table where the optional did not match");
            Assert.That(velocities, Is.EquivalentTo(new[] { 7 }));
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Or_matches_entities_that_have_either_term()
    {
        using World world = new();
        Entity onlyPosition = world.CreateEntity();
        Entity onlyVelocity = world.CreateEntity();
        Entity both = world.CreateEntity();
        Entity neither = world.CreateEntity();
        world.Set(onlyPosition, new Position { X = 1 });
        world.Set(onlyVelocity, new Velocity { X = 2 });
        world.Set(both, new Position { X = 3 });
        world.Set(both, new Velocity { X = 4 });

        Query query = world.CreateQuery().With<Position>().Or().With<Velocity>().BuildUncached();

        var matched = new List<ulong>();
        foreach (TableView table in query)
            foreach (Entity entity in table.Entities)
                matched.Add(entity);

        Assert.That(matched, Is.EquivalentTo(new[] { (ulong)onlyPosition, (ulong)onlyVelocity, (ulong)both }));
        world.DestroyQuery(query);
    }

    [Test]
    public void Count_and_entities_report_the_table_rows()
    {
        using World world = new();
        Entity a = world.CreateEntity();
        Entity b = world.CreateEntity();
        world.Set(a, new Position { X = 1 });
        world.Set(b, new Position { X = 2 });

        Query query = world.CreateQuery().With<Position>().BuildUncached();

        var entities = new List<ulong>();
        int total = 0;
        foreach (TableView table in query)
        {
            total += table.Count;
            foreach (Entity entity in table.Entities)
                entities.Add(entity);
        }

        Assert.Multiple(() =>
        {
            Assert.That(total, Is.EqualTo(2));
            Assert.That(entities, Is.EquivalentTo(new[] { (ulong)a, (ulong)b }));
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void A_query_that_matches_nothing_yields_no_tables()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });

        Query query = world.CreateQuery().With<Velocity>().BuildUncached();

        int tables = 0;
        foreach (TableView _ in query)
            tables++;

        Assert.That(tables, Is.Zero);
        world.DestroyQuery(query);
    }

    [Test]
    public void With_id_and_pair_overloads_match_by_id()
    {
        using World world = new();
        Entity likes = world.CreateEntity();
        Entity apples = world.CreateEntity();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 5 });
        world.AddPair(e, likes, apples);

        Id pair = world.Pair(likes, apples);
        Query query = world.CreateQuery().With<Position>().With(likes, apples).BuildUncached();

        var seen = new List<int>();
        foreach (TableView table in query)
        {
            Assert.That(table.HasField(pair), Is.False, "a pair tag carries no data");
            ReadOnlySpan<Position> positions = table.GetFieldSpan<Position>();
            for (int row = 0; row < table.Count; row++)
                seen.Add(positions[row].X);
        }

        Assert.That(seen, Is.EquivalentTo(new[] { 5 }));
        world.DestroyQuery(query);
    }

    [Test]
    public void Cached_and_uncached_queries_iterate_identically()
    {
        using World world = new();
        for (int i = 0; i < 5; i++)
            world.Set(world.CreateEntity(), new Position { X = i });

        Query cached = world.CreateQuery().With<Position>().BuildCached();
        Query uncached = world.CreateQuery().With<Position>().BuildUncached();

        Assert.That(Collect(cached), Is.EquivalentTo(Collect(uncached)));

        world.DestroyQuery(cached);
        world.DestroyQuery(uncached);

        static List<int> Collect(Query query)
        {
            var values = new List<int>();
            foreach (TableView table in query)
            {
                ReadOnlySpan<Position> positions = table.GetFieldSpan<Position>();
                for (int row = 0; row < table.Count; row++)
                    values.Add(positions[row].X);
            }

            return values;
        }
    }

    // --- Storage shapes ---

    [Test]
    public void Sparse_field_reads_distinct_values_per_row()
    {
        using World world = new();
        Entity a = world.CreateEntity();
        Entity b = world.CreateEntity();
        world.Set(a, new SparseValue { Value = 11 });
        world.Set(b, new SparseValue { Value = 22 });

        Query query = world.CreateQuery().With<SparseValue>().BuildUncached();

        var seen = new List<int>();
        bool reportedSparse = true;
        foreach (TableView table in query)
        {
            reportedSparse &= table.HasSparseField<SparseValue>();
            for (int row = 0; row < table.Count; row++)
                seen.Add(table.GetField<SparseValue>(row).Value);
        }

        Assert.Multiple(() =>
        {
            Assert.That(reportedSparse, Is.True);
            Assert.That(seen, Is.EquivalentTo(new[] { 11, 22 }));
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Inherited_component_is_read_as_a_shared_field()
    {
        using World world = new();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 42 });

        // The instance inherits Inherited from the base and does not own it, so the
        // field resolves as shared rather than owned on the instance's table.
        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);

        Query query = world.CreateQuery().With<Inherited>().BuildUncached();

        bool sawShared = false;
        bool sawOwned = false;
        int sharedValue = 0;
        var sharedPerRow = new List<int>();
        foreach (TableView table in query)
        {
            if (table.HasSharedField<Inherited>())
            {
                sawShared = true;
                sharedValue = table.GetSharedField<Inherited>().Value;
                for (int row = 0; row < table.Count; row++)
                    sharedPerRow.Add(table.GetField<Inherited>(row).Value);
            }
            else
            {
                sawOwned = true;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(sawShared, Is.True, "the instance's table reads Inherited as shared");
            Assert.That(sawOwned, Is.True, "the base's table owns Inherited");
            Assert.That(sharedValue, Is.EqualTo(42));
            Assert.That(sharedPerRow, Is.All.EqualTo(42));
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void None_term_matches_without_producing_data()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.Set(e, new Velocity { X = 2 });

        Query query = world.CreateQuery()
            .With<Position>()
            .With<Velocity>().None()
            .BuildUncached();

        bool matched = false;
        foreach (TableView table in query)
        {
            matched = true;
            Assert.That(table.HasField<Position>(), Is.True);
            Assert.That(table.HasField<Velocity>(), Is.False);
            Assert.That(table.TryGetFieldSpan(out ReadOnlySpan<Velocity> _), Is.False);
        }

        Assert.That(matched, Is.True);
        world.DestroyQuery(query);
    }

    [Test]
    public void A_zero_size_tag_carries_no_data()
    {
        using World world = new();
        Entity tag = world.CreateEntity();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.Add(e, tag);

        Query query = world.CreateQuery().With<Position>().With(tag).BuildUncached();

        bool matched = false;
        foreach (TableView table in query)
        {
            matched = true;
            Assert.That(table.HasField(tag), Is.False);
            Assert.That(table.TryGetFieldSpan<Position>(tag, out _), Is.False);
        }

        Assert.That(matched, Is.True);
        world.DestroyQuery(query);
    }

    // --- Read/write enforcement ---

    [Test]
    public void An_in_field_is_readable_through_the_read_only_accessors()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 7 });

        Query query = world.CreateQuery().With<Position>().In().BuildUncached();

        var seen = new List<int>();
        foreach (TableView table in query)
        {
            ReadOnlySpan<Position> span = table.GetFieldSpan<Position>();
            for (int row = 0; row < table.Count; row++)
            {
                Assert.That(table.GetField<Position>(row).X, Is.EqualTo(span[row].X));
                seen.Add(span[row].X);
            }
        }

        Assert.That(seen, Is.EquivalentTo(new[] { 7 }));
        world.DestroyQuery(query);
    }

    [Test]
    public void The_read_only_accessors_also_read_a_writable_field()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 9 });

        Query query = world.CreateQuery().With<Position>().BuildUncached();

        var seen = new List<int>();
        foreach (TableView table in query)
        {
            ReadOnlySpan<Position> span = table.GetFieldSpan<Position>();
            for (int row = 0; row < table.Count; row++)
                seen.Add(span[row].X);
        }

        Assert.That(seen, Is.EquivalentTo(new[] { 9 }));
        world.DestroyQuery(query);
    }

    [Test]
    public void A_writable_shared_field_can_be_mutated_through_the_mut_accessor()
    {
        using World world = new();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 42 });
        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);

        using (DisposableQuery query = world.CreateQuery().With<Inherited>().BuildDisposable())
            foreach (TableView table in query)
                if (table.HasSharedField<Inherited>())
                    table.GetSharedFieldMut<Inherited>().Value += 100;

        Assert.That(world.Get<Inherited>(baseEntity).Value, Is.EqualTo(142));
    }

    // --- Id accessor overloads ---

    [Test]
    public void Field_id_overloads_read_and_write_an_owned_field()
    {
        using World world = new();
        Id position = world.Component<Position>();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 5 });

        using (DisposableQuery query = world.CreateQuery().With<Position>().BuildDisposable())
            foreach (TableView table in query)
            {
                ReadOnlySpan<Position> span = table.GetFieldSpan<Position>(position);
                Assert.That(span[0].X, Is.EqualTo(5));
                Assert.That(table.GetField<Position>(position, 0).X, Is.EqualTo(5));

                table.GetFieldSpanMut<Position>(position)[0].X += 10;
                table.GetFieldMut<Position>(position, 0).X += 100;
            }

        Assert.That(world.Get<Position>(e).X, Is.EqualTo(115));
    }

    [Test]
    public void Shared_field_id_overloads_read_and_write()
    {
        using World world = new();
        Id inherited = world.Component<Inherited>();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 42 });
        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);

        using (DisposableQuery query = world.CreateQuery().With<Inherited>().BuildDisposable())
            foreach (TableView table in query)
                if (table.HasSharedField<Inherited>())
                {
                    Assert.That(table.GetSharedField<Inherited>(inherited).Value, Is.EqualTo(42));
                    table.GetSharedFieldMut<Inherited>(inherited).Value += 100;
                }

        Assert.That(world.Get<Inherited>(baseEntity).Value, Is.EqualTo(142));
    }

    [Test]
    public void Trying_an_absent_in_optional_through_the_id_mut_overload_returns_false()
    {
        using World world = new();
        Id velocity = world.Component<Velocity>();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().Optional<Velocity>().In().BuildUncached();

        bool matched = false;
        foreach (TableView table in query)
        {
            matched = true;
            Assert.That(table.TryGetFieldSpanMut<Velocity>(velocity, out Span<Velocity> span), Is.False);
            Assert.That(span.IsEmpty, Is.True);
        }

        Assert.That(matched, Is.True);
        world.DestroyQuery(query);
    }

    // --- Lifetime ---

    [Test]
    public void A_cached_query_forced_to_downgrade_leaves_no_orphan_entity()
    {
        // A single optional term is not cacheable, so flecs downgrades the cache to
        // uncached. The builder still pre-creates the cache entity to survive that
        // downgrade, so destroying the query must free it rather than leak it.
        using World world = new();
        Query query = world.CreateQuery().Optional<SparseValue>().BuildCached();

        Entity entity = query.Entity;
        Assert.That((ulong)entity, Is.Not.Zero, "a cached build pre-creates a query entity");
        Assert.That(world.EntityIsAlive(entity), Is.True);

        world.DestroyQuery(query);

        Assert.That(world.EntityIsAlive(entity), Is.False, "the query entity was freed");
    }

    [Test]
    public void DestroyQuery_frees_a_persisted_query()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().BuildUncached();

        foreach (TableView _ in query) { }

        Assert.DoesNotThrow(() => world.DestroyQuery(query));
    }

    [Test]
    public void DestroyQuery_of_a_default_query_is_a_no_op()
    {
        using World world = new();
        Assert.DoesNotThrow(() => world.DestroyQuery(default));
    }

    [Test]
    public void A_persisted_query_can_be_re_iterated()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        world.Set(world.CreateEntity(), new Position { X = 2 });

        Query query = world.CreateQuery().With<Position>().BuildCached();

        Assert.That(CountRows(query), Is.EqualTo(2));
        Assert.That(CountRows(query), Is.EqualTo(2));

        world.DestroyQuery(query);

        static int CountRows(Query query)
        {
            int total = 0;
            foreach (TableView table in query)
                total += table.Count;
            return total;
        }
    }

    // --- Enumerator lifecycle ---

    [Test]
    public void Breaking_out_of_iteration_disposes_the_iterator()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().BuildUncached();

        Assert.DoesNotThrow(() =>
        {
            foreach (TableView table in query)
            {
                _ = table.Count;
                break;
            }
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void An_enumerator_disposed_without_advancing_does_not_throw()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().BuildUncached();

        // Finalizing an iterator that was never advanced must not throw.
        Query.Enumerator enumerator = query.GetEnumerator();
        enumerator.Dispose();

        world.DestroyQuery(query);
    }

    [Test]
    public void Disposing_and_advancing_are_idempotent_after_completion()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().BuildUncached();

        Query.Enumerator enumerator = query.GetEnumerator();
        while (enumerator.MoveNext()) { }

        // Advancing after completion and disposing twice must not re-enter the
        // native iterator; a second fini would be undefined.
        bool advancedAfterCompletion = enumerator.MoveNext();
        enumerator.Dispose();
        enumerator.Dispose();
        bool advancedAfterDispose = enumerator.MoveNext();

        Assert.Multiple(() =>
        {
            Assert.That(advancedAfterCompletion, Is.False, "advancing after completion");
            Assert.That(advancedAfterDispose, Is.False, "advancing after disposal");
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Disposing_a_disposable_query_twice_does_not_double_free()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });

        DisposableQuery query = world.CreateQuery().With<Position>().BuildDisposable();
        query.Dispose();
        query.Dispose();
    }

    // --- Traversal and positional reads ---

    [Test]
    public void Up_reads_an_inherited_component_from_the_parent()
    {
        using World world = new();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Velocity { X = 7 });
        Entity child = world.CreateEntity();
        world.Set(child, new Position { X = 3 });
        world.AddChildOf(child, parent);

        Query query = world.CreateQuery().With<Position>().With<Velocity>().Up().BuildUncached();

        bool matchedChild = false;
        foreach (TableView table in query)
        {
            Assert.That(table.HasSharedField<Position>(), Is.False, "Position is owned by the child");
            Assert.That(table.HasSharedField<Velocity>(), Is.True, "Velocity is inherited from the parent");
            Assert.That(table.GetSharedField<Velocity>().X, Is.EqualTo(7));
            for (int row = 0; row < table.Count; row++)
            {
                matchedChild = true;
                Assert.That(table.GetField<Position>(row).X, Is.EqualTo(3));
            }
        }

        Assert.That(matchedChild, Is.True, "the child matches via up-sourced Velocity");
        world.DestroyQuery(query);
    }

    [Test]
    public void Up_only_does_not_match_the_owner_itself()
    {
        using World world = new();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Velocity { X = 7 });
        Entity child = world.CreateEntity();
        world.AddChildOf(child, parent);

        Query query = world.CreateQuery().With<Velocity>().Up().BuildUncached();

        var matched = new List<Entity>();
        foreach (TableView table in query)
            foreach (Entity e in table.Entities)
                matched.Add(e);

        Assert.Multiple(() =>
        {
            Assert.That(matched, Does.Contain(child), "the child matches via its parent's Velocity");
            Assert.That(matched, Does.Not.Contain(parent), "up-only excludes the owner of Velocity");
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Self_or_up_matches_owner_and_descendants()
    {
        using World world = new();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Velocity { X = 7 });
        Entity child = world.CreateEntity();
        world.AddChildOf(child, parent);

        Query query = world.CreateQuery().With<Velocity>().Self().Up().BuildUncached();

        var owned = new List<Entity>();
        var shared = new List<Entity>();
        foreach (TableView table in query)
        {
            bool isShared = table.HasSharedField<Velocity>();
            foreach (Entity e in table.Entities)
                (isShared ? shared : owned).Add(e);
        }

        Assert.Multiple(() =>
        {
            Assert.That(owned, Does.Contain(parent), "the parent owns Velocity (self)");
            Assert.That(shared, Does.Contain(child), "the child inherits Velocity (up)");
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void UpAncestorsFirst_orders_ancestors_before_descendants()
    {
        using World world = new();
        Entity root = world.CreateEntity();
        world.Set(root, new Position { X = 0 });
        Entity mid = world.CreateEntity();
        world.Set(mid, new Position { X = 1 });
        world.AddChildOf(mid, root);
        Entity leaf = world.CreateEntity();
        world.Set(leaf, new Position { X = 2 });
        world.AddChildOf(leaf, mid);

        // Term 0 is each node's own Position. Term 1 sources the parent's Position with
        // UpAncestorsFirst so the cache is ordered shallow-to-deep. Optional lets the root match.
        Query query = world.CreateQuery()
            .With<Position>()
            .Optional<Position>().UpAncestorsFirst()
            .BuildCached();

        var order = new List<int>();
        foreach (TableView table in query)
        {
            ReadOnlySpan<Position> self = table.GetFieldSpan<Position>(0);
            for (int row = 0; row < table.Count; row++)
                order.Add(self[row].X);
        }

        Assert.That(order, Is.EqualTo(new[] { 0, 1, 2 }), "tables are visited shallow-to-deep");
        world.DestroyQuery(query);
    }

    [Test]
    public void UpDescendantsFirst_orders_descendants_before_ancestors()
    {
        using World world = new();
        Entity root = world.CreateEntity();
        world.Set(root, new Position { X = 0 });
        Entity mid = world.CreateEntity();
        world.Set(mid, new Position { X = 1 });
        world.AddChildOf(mid, root);
        Entity leaf = world.CreateEntity();
        world.Set(leaf, new Position { X = 2 });
        world.AddChildOf(leaf, mid);

        Query query = world.CreateQuery()
            .With<Position>()
            .Optional<Position>().UpDescendantsFirst()
            .BuildCached();

        var order = new List<int>();
        foreach (TableView table in query)
        {
            ReadOnlySpan<Position> self = table.GetFieldSpan<Position>(0);
            for (int row = 0; row < table.Count; row++)
                order.Add(self[row].X);
        }

        Assert.That(order, Is.EqualTo(new[] { 2, 1, 0 }), "UpDescendantsFirst reverses the UpAncestorsFirst order");
        world.DestroyQuery(query);
    }

    [Test]
    public void UpAncestorsFirst_on_an_uncached_query_fails_to_build()
    {
        using World world = new();
        Assert.Throws<InvalidOperationException>(() =>
            world.CreateQuery().With<Position>().Optional<Position>().UpAncestorsFirst().BuildUncached());
    }

    [Test]
    public void UpDescendantsFirst_on_an_uncached_query_fails_to_build()
    {
        using World world = new();
        Assert.Throws<InvalidOperationException>(() =>
            world.CreateQuery().With<Position>().Optional<Position>().UpDescendantsFirst().BuildUncached());
    }

    [Test]
    public void Source_reads_a_fixed_entity_for_every_row()
    {
        using World world = new();
        Entity cfg = world.CreateEntity();
        world.Set(cfg, new Velocity { X = 99 });

        Entity a = world.CreateEntity();
        world.Set(a, new Position { X = 1 });
        Entity b = world.CreateEntity();
        world.Set(b, new Position { X = 2 });

        Query query = world.CreateQuery().With<Position>().With<Velocity>().Source(cfg).BuildUncached();

        var positions = new List<int>();
        foreach (TableView table in query)
        {
            Assert.That(table.HasSharedField<Velocity>(), Is.True, "the fixed source reads as shared");
            Assert.That(table.GetSharedField<Velocity>().X, Is.EqualTo(99));
            ReadOnlySpan<Position> ps = table.GetFieldSpan<Position>();
            for (int row = 0; row < table.Count; row++)
                positions.Add(ps[row].X);
        }

        Assert.That(positions, Is.EquivalentTo(new[] { 1, 2 }));
        world.DestroyQuery(query);
    }

    [Test]
    public void A_fixed_source_marked_InOut_is_writable_and_mutates_the_source()
    {
        using World world = new();
        Entity cfg = world.CreateEntity();
        world.Set(cfg, new Velocity { X = 1 });
        Entity a = world.CreateEntity();
        world.Set(a, new Position { X = 0 });

        // Velocity is sourced from the fixed cfg entity. A fixed source is read-only by
        // default, but marking the term InOut() makes it writable.
        Query query = world.CreateQuery().With<Position>().With<Velocity>().Source(cfg).InOut().BuildUncached();

        foreach (TableView table in query)
            table.GetSharedFieldMut<Velocity>(1).X = 42;

        Assert.That(world.Get<Velocity>(cfg).X, Is.EqualTo(42),
            "writing through an InOut fixed source mutates the source entity");
        world.DestroyQuery(query);
    }

#if DEBUG
    [Test]
    public void Mutating_a_default_access_fixed_source_throws_in_debug()
    {
        using World world = new();
        Entity cfg = world.CreateEntity();
        world.Set(cfg, new Velocity { X = 1 });
        Entity a = world.CreateEntity();
        world.Set(a, new Position { X = 0 });

        // No InOut(): a fixed source is read-only by default.
        Query query = world.CreateQuery().With<Position>().With<Velocity>().Source(cfg).BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetSharedFieldMut<Velocity>(1);
        });
        world.DestroyQuery(query);
    }
#endif

    [Test]
    public void Up_with_an_explicit_relationship_traverses_it()
    {
        using World world = new();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 5 });
        world.Set(baseEntity, new Position { X = 0 });

        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);
        world.Set(instance, new Position { X = 1 });

        Query query = world.CreateQuery().With<Position>().With<Inherited>().Up(world.IsA).BuildUncached();

        var matched = new List<Entity>();
        foreach (TableView table in query)
        {
            Assert.That(table.HasSharedField<Inherited>(), Is.True, "Inherited comes from the base via IsA");
            Assert.That(table.GetSharedField<Inherited>().Value, Is.EqualTo(5));
            foreach (Entity e in table.Entities)
                matched.Add(e);
        }

        Assert.Multiple(() =>
        {
            Assert.That(matched, Does.Contain(instance), "the instance matches via IsA-up");
            Assert.That(matched, Does.Not.Contain(baseEntity), "up-only excludes the base itself");
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Reads_self_and_parent_transform_end_to_end()
    {
        using World world = new();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Position { X = 100 });
        Entity child = world.CreateEntity();
        world.Set(child, new Position { X = 1 });
        world.AddChildOf(child, parent);

        Query query = world.CreateQuery().With<Position>().With<Position>().Up().BuildUncached();

        bool matched = false;
        foreach (TableView table in query)
        {
            matched = true;
            Assert.That(table.IsFieldShared(0), Is.False, "field 0 is the child's own Position");
            Assert.That(table.IsFieldShared(1), Is.True, "field 1 is the parent's Position (up)");
            Assert.That(table.GetFieldSpan<Position>(0)[0].X, Is.EqualTo(1));
            Assert.That(table.GetSharedField<Position>(1).X, Is.EqualTo(100));
        }

        Assert.That(matched, Is.True, "the child reads its own and its parent's Position");
        world.DestroyQuery(query);
    }

    [Test]
    public void Positional_accessors_read_a_duplicate_id_query()
    {
        using World world = new();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Position { X = 42 });
        Entity child = world.CreateEntity();
        world.Set(child, new Position { X = 7 });
        world.AddChildOf(child, parent);

        Query query = world.CreateQuery().With<Position>().With<Position>().Up().BuildUncached();

        bool matched = false;
        foreach (TableView table in query)
        {
            matched = true;

            Assert.That(table.HasSharedField<Position>(), Is.False, "type-resolution picks field 0 (owned)");
            Assert.That(table.GetFieldSpan<Position>()[0].X, Is.EqualTo(7), "type-resolved span is field 0");
            Assert.That(table.GetSharedField<Position>(1).X, Is.EqualTo(42), "field 1 is the parent's");
        }

        Assert.That(matched, Is.True);
        world.DestroyQuery(query);
    }

    [Test]
    public void GetFieldId_and_GetFieldTarget_discover_a_wildcard_match()
    {
        using World world = new();
        Entity likes = world.CreateEntity();
        Entity apples = world.CreateEntity();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.AddPair(e, likes, apples);

        Query query = world.CreateQuery().With<Position>().With(likes, EcsWildcard).BuildUncached();

        bool matched = false;
        foreach (TableView table in query)
        {
            matched = true;
            Assert.That(table.GetFieldId(1), Is.EqualTo(world.Pair(likes, apples)),
                "the wildcard resolved to (likes, apples)");
            Assert.That(table.GetFieldTarget(1), Is.EqualTo(apples), "the matched target is apples");
        }

        Assert.That(matched, Is.True);
        world.DestroyQuery(query);
    }

    [Test]
    public void Positional_predicates_match_typed_and_are_range_safe()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.Set(e, new SparseValue { Value = 2 });

        Query query = world.CreateQuery().With<Position>().With<SparseValue>().BuildUncached();

        bool matched = false;
        foreach (TableView table in query)
        {
            matched = true;
            Assert.That(table.HasField(0), Is.EqualTo(table.HasField<Position>()));
            Assert.That(table.IsFieldShared(0), Is.EqualTo(table.HasSharedField<Position>()));
            Assert.That(table.IsFieldSparse(0), Is.EqualTo(table.HasSparseField<Position>()));
            Assert.That(table.HasField(1), Is.EqualTo(table.HasField<SparseValue>()));
            Assert.That(table.IsFieldSparse(1), Is.EqualTo(table.HasSparseField<SparseValue>()));
            Assert.That(table.IsFieldSparse(1), Is.True, "SparseValue is a sparse field");

            Assert.That(table.HasField(99), Is.False);
            Assert.That(table.IsFieldShared(99), Is.False);
            Assert.That(table.IsFieldSparse(99), Is.False);
        }

        Assert.That(matched, Is.True);
        world.DestroyQuery(query);
    }

    // --- Builder misuse ---

    [Test]
    public void Refining_before_adding_a_term_throws()
    {
        using World world = new();
        Assert.Throws<InvalidOperationException>(() => world.CreateQuery().In());
    }

    [Test]
    public void Or_before_adding_a_term_throws()
    {
        using World world = new();
        Assert.Throws<InvalidOperationException>(() => world.CreateQuery().Or());
    }

    [Test]
    public void Traversal_verb_before_a_term_throws()
    {
        using World world = new();
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => world.CreateQuery().Self());
            Assert.Throws<InvalidOperationException>(() => world.CreateQuery().Up());
            Assert.Throws<InvalidOperationException>(() => world.CreateQuery().UpAncestorsFirst());
            Assert.Throws<InvalidOperationException>(() => world.CreateQuery().UpDescendantsFirst());
            Assert.Throws<InvalidOperationException>(() => world.CreateQuery().Source(world.CreateEntity()));
        });
    }

#if DEBUG
    // --- Builder and query misuse ---

    [Test]
    public void Adding_a_zero_id_term_throws_in_debug()
    {
        using World world = new();

        // A zero id (Id.None, or a failed Lookup) would silently truncate the
        // query at that term, so we must reject it instead.
        Assert.Throws<InvalidOperationException>(() => world.CreateQuery().With(Id.None));
    }

    [Test]
    public void Reusing_a_built_builder_throws_in_debug()
    {
        using World world = new();

        // Prohibit re-use of QueryBuilder instances
        Assert.Throws<InvalidOperationException>(() =>
        {
            QueryBuilder builder = world.CreateQuery();
            builder.With<Position>();
            builder.BuildCached();
            builder.BuildCached();
        });
    }

    [Test]
    public void Iterating_a_default_query_throws_in_debug()
    {
        // A default query has a null handle. Iterating it would abort in the
        // native iterator, so GetEnumerator guards it first.
        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView _ in default(Query)) { }
        });
    }

    // --- Accessor misuse ---

    [Test]
    public void Reading_a_sparse_field_as_a_span_throws_in_debug()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new SparseValue { Value = 1 });
        Query query = world.CreateQuery().With<SparseValue>().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldSpan<SparseValue>();
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Reading_a_shared_field_as_a_span_throws_in_debug()
    {
        using World world = new();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 42 });
        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);

        Query query = world.CreateQuery().With<Inherited>().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                if (table.HasSharedField<Inherited>())
                    _ = table.GetFieldSpan<Inherited>();
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Reading_a_none_field_throws_in_debug()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.Set(e, new Velocity { X = 2 });
        Query query = world.CreateQuery().With<Position>().With<Velocity>().None().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldSpan<Velocity>();
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Reading_a_field_the_query_does_not_select_throws_in_debug()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldSpan<Velocity>();
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Reading_a_row_out_of_range_throws_in_debug()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetField<Position>(table.Count);
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Reading_a_field_as_a_wrong_sized_type_throws_in_debug()
    {
        using World world = new();
        Id position = world.Component<Position>();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().BuildUncached();

        // The field stores a 4-byte Position; reading it as an 8-byte long is a
        // size mismatch.
        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldSpan<long>(position);
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Reading_a_field_as_a_wrong_same_sized_type_throws_in_debug()
    {
        using World world = new();
        Id position = world.Component<Position>();
        world.Component<Velocity>();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().BuildUncached();

        // Position and Velocity are both 4 bytes, so only the type-identity 
        // check catches reading the Position field as Velocity.
        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldSpan<Velocity>(position);
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Reading_an_owned_field_as_shared_throws_in_debug()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Inherited { Value = 1 });
        Query query = world.CreateQuery().With<Inherited>().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetSharedField<Inherited>();
        });
        world.DestroyQuery(query);
    }

    // --- Read/write enforcement misuse ---

    [Test]
    public void Mutably_reading_an_in_field_as_a_span_throws_in_debug()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().In().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldSpanMut<Position>();
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Mutably_reading_an_in_field_per_row_throws_in_debug()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().In().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldMut<Position>(0);
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Mutably_reading_an_in_shared_field_throws_in_debug()
    {
        using World world = new();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 42 });
        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);

        Query query = world.CreateQuery().With<Inherited>().In().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                if (table.HasSharedField<Inherited>())
                    _ = table.GetSharedFieldMut<Inherited>();
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Mutably_trying_a_present_in_field_throws_in_debug()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.Set(e, new Velocity { X = 2 });

        Query query = world.CreateQuery().With<Position>().Optional<Velocity>().In().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                if (table.HasField<Velocity>())
                    _ = table.TryGetFieldSpanMut<Velocity>(out _);
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Mutably_reading_an_in_field_through_the_id_overload_throws_in_debug()
    {
        using World world = new();
        Id position = world.Component<Position>();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().In().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldSpanMut<Position>(position);
        });
        world.DestroyQuery(query);
    }

    // --- Positional and traversal misuse ---

    [Test]
    public void Positional_field_index_out_of_range_throws_in_debug()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldSpan<Position>(99);
        });
        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldSpan<Position>(-1);
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void GetFieldTarget_on_a_non_pair_field_throws_in_debug()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });
        Query query = world.CreateQuery().With<Position>().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldTarget(0);
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Source_with_a_zero_entity_throws_in_debug()
    {
        using World world = new();
        Assert.Throws<InvalidOperationException>(() =>
            world.CreateQuery().With<Position>().Source(Entity.None));
    }

    [Test]
    public void Source_with_a_pair_id_throws_in_debug()
    {
        using World world = new();
        Entity likes = world.CreateEntity();
        Entity apples = world.CreateEntity();
        Id pair = world.Pair(likes, apples);

        Assert.Throws<InvalidOperationException>(() =>
            world.CreateQuery().With<Position>().Source(pair));
    }

    [Test]
    public void Traversal_with_a_zero_relationship_throws_in_debug()
    {
        using World world = new();

        Assert.Throws<InvalidOperationException>(() =>
            world.CreateQuery().With<Position>().Up(Id.None));
        Assert.Throws<InvalidOperationException>(() =>
            world.CreateQuery().With<Position>().UpAncestorsFirst(Id.None));
    }

    [Test]
    public void Traversal_with_a_non_traversable_relationship_throws_in_debug()
    {
        using World world = new();
        Entity plain = world.CreateEntity();

        Assert.Throws<InvalidOperationException>(() =>
            world.CreateQuery().With<Position>().Up(plain));
        Assert.Throws<InvalidOperationException>(() =>
            world.CreateQuery().With<Position>().UpAncestorsFirst(plain));
    }

    [Test]
    public void Traversal_with_a_traversable_relationship_is_allowed()
    {
        using World world = new();
        Entity rel = world.CreateEntity();
        world.Add(rel, EcsTraversable);

        Query query = world.CreateQuery().With<Position>().Up(rel).BuildUncached();
        world.DestroyQuery(query);
    }

    [Test]
    public void Mutating_an_up_sourced_field_throws_in_debug()
    {
        using World world = new();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Velocity { X = 7 });
        Entity child = world.CreateEntity();
        world.Set(child, new Position { X = 1 });
        world.AddChildOf(child, parent);

        Query query = world.CreateQuery().With<Position>().With<Velocity>().Up().BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                if (table.IsFieldShared(1))
                    _ = table.GetSharedFieldMut<Velocity>(1);
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Mutating_an_in_field_after_an_or_group_throws_in_debug()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });       
        world.Set(e, new Inherited { Value = 5 });  

        // (Position OR Velocity) -> field 0; Inherited -> field 1 but term-array index 2.
        Query query = world.CreateQuery()
            .With<Position>().Or().With<Velocity>()
            .With<Inherited>().In()
            .BuildUncached();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (TableView table in query)
                _ = table.GetFieldSpanMut<Inherited>();
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Mutating_a_writable_field_after_an_or_group_does_not_throw()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });       
        world.Set(e, new Inherited { Value = 5 });  

        Query query = world.CreateQuery()
            .With<Position>().Or().With<Velocity>()
            .With<Inherited>()
            .BuildUncached();

        foreach (TableView table in query)
            _ = table.GetFieldSpanMut<Inherited>();
        world.DestroyQuery(query);
    }
#endif

    // --- test components ---

    private struct Position
    {
        public int X;
    }

    private struct Velocity
    {
        public int X;
    }

    [Sparse]
    private struct SparseValue
    {
        public int Value;
    }

    [OnInstantiate(Instantiate.Inherit)]
    private struct Inherited
    {
        public int Value;
    }
}
