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
            Span<Position> positions = table.GetFieldSpan<Position>();
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
            Span<Position> positions = table.GetFieldSpan<Position>();
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
                Span<Position> positions = table.GetFieldSpan<Position>();
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
            Span<Position> positions = table.GetFieldSpan<Position>();
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
                Assert.That(table.TryGetFieldSpan(out Span<Velocity> v), Is.True);
                for (int row = 0; row < table.Count; row++)
                    velocities.Add(v[row].X);
            }
            else
            {
                sawAbsent = true;
                Assert.That(table.TryGetFieldSpan(out Span<Velocity> _), Is.False);
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
            Span<Position> positions = table.GetFieldSpan<Position>();
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
                Span<Position> positions = table.GetFieldSpan<Position>();
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
            reportedSparse &= table.IsFieldSparse<SparseValue>();
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
            if (table.IsFieldShared<Inherited>())
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
            Assert.That(table.TryGetFieldSpan(out Span<Velocity> _), Is.False);
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
    public void Reusing_a_builder_builds_independent_queries()
    {
        using World world = new();
        world.Set(world.CreateEntity(), new Position { X = 1 });

        QueryBuilder builder = world.CreateQuery();
        builder.With<Position>();

        Query first = builder.BuildCached();
        Assert.That(CountRows(first), Is.EqualTo(1));

        // Building a second query from the same builder must not rebind or tear
        // down the first: flecs unbinds any query already on desc.entity, so a
        // stale entity carried over would break the first query here.
        Query second = builder.BuildCached();

        Assert.Multiple(() =>
        {
            Assert.That(CountRows(first), Is.EqualTo(1), "the first query still works");
            Assert.That(CountRows(second), Is.EqualTo(1), "the second query works");
        });

        world.DestroyQuery(first);
        world.DestroyQuery(second);

        static int CountRows(Query query)
        {
            int total = 0;
            foreach (TableView table in query)
                total += table.Count;
            return total;
        }
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

    // --- Builder misuse ---

    [Test]
    public void Refining_before_adding_a_term_throws()
    {
        using World world = new();
        Assert.Throws<InvalidOperationException>(() => world.CreateQuery().In());
    }

#if DEBUG
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
                if (table.IsFieldShared<Inherited>())
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
